using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace Unity.Robotics.PickAndPlace
{
    /// <summary>
    ///     UC 5 - Bin picking and sorting.
    ///     Runs pick-and-place cycles, either one at a time or for every object in
    ///     the bin, and measures how many cycles succeed and how long they take.
    ///     A cycle ends as soon as the robot has come to a stop: the object is then
    ///     either inside the success radius of its container (success) or not (failure),
    ///     so a failed grasp is reported immediately instead of waiting for the timeout.
    /// </summary>
    public class PickAndPlaceManager : MonoBehaviour
    {
        [SerializeField]
        TrajectoryPlanner m_TrajectoryPlanner;

        [Tooltip("The robot, used to detect when it has stopped moving.")]
        [SerializeField]
        GameObject m_NiryoOne;

        [Tooltip("Objects in the bin, in the same order as the dropdown options.")]
        [SerializeField]
        GameObject[] m_Objects;

        [Tooltip("Sorting container for each object, in the same order.")]
        [SerializeField]
        GameObject[] m_Boxes;

        [Tooltip("UI text that shows the statistics. Optional.")]
        [SerializeField]
        Text m_StatsText;

        [Tooltip("Distance to the container below which the placement counts as successful, in meters.")]
        [SerializeField]
        float m_SuccessRadius = 0.06f;

        [Tooltip("Hard limit for one cycle, used only if the robot never stops, in seconds.")]
        [SerializeField]
        float m_CycleTimeout = 40.0f;

        [Tooltip("How long the robot must stay still before the cycle is considered finished, in seconds.")]
        [SerializeField]
        float m_StillTime = 1.5f;

        [Tooltip("How long to wait for the robot to start moving after the request, in seconds.")]
        [SerializeField]
        float m_StartGrace = 8.0f;

        [Tooltip("Joint speed below which the robot counts as standing still, in rad/s.")]
        [SerializeField]
        float m_StillThreshold = 0.05f;

        [Tooltip("Pause between two cycles in automatic mode, in seconds.")]
        [SerializeField]
        float m_PauseBetweenCycles = 1.5f;

        readonly List<ArticulationBody> m_Joints = new List<ArticulationBody>();

        int m_Cycles;
        int m_Successes;
        float m_TotalTime;
        float m_LastCycleTime;
        bool m_Busy;

        void Start()
        {
            CacheJoints();
            UpdateStatsText("Ready.");
        }

        void CacheJoints()
        {
            m_Joints.Clear();

            if (m_NiryoOne == null)
            {
                Debug.LogWarning("PickAndPlaceManager: Niryo One is not assigned, " +
                                 "so cycles will end on the timeout instead of when the robot stops.");
                return;
            }

            foreach (var body in m_NiryoOne.GetComponentsInChildren<ArticulationBody>())
            {
                if (body.jointType != ArticulationJointType.FixedJoint)
                {
                    m_Joints.Add(body);
                }
            }
        }

        /// <summary>
        ///     Runs one cycle for the object at the given index.
        /// </summary>
        public void RunCycle(int index)
        {
            if (m_Busy)
            {
                Debug.LogWarning("PickAndPlaceManager: a cycle is already running.");
                return;
            }

            if (!IsReady(index))
            {
                return;
            }

            StartCoroutine(RunCycleRoutine(index));
        }

        /// <summary>
        ///     Runs one cycle for every object in the bin, one after the other.
        /// </summary>
        public void RunAll()
        {
            if (m_Busy)
            {
                Debug.LogWarning("PickAndPlaceManager: a cycle is already running.");
                return;
            }

            StartCoroutine(RunAllRoutine());
        }

        /// <summary>
        ///     Clears the "busy" state after an interrupted cycle.
        /// </summary>
        public void Abort()
        {
            StopAllCoroutines();
            m_Busy = false;
            UpdateStatsText("Aborted.");
        }

        /// <summary>
        ///     Clears the statistics.
        /// </summary>
        public void ResetStatistics()
        {
            m_Cycles = 0;
            m_Successes = 0;
            m_TotalTime = 0.0f;
            m_LastCycleTime = 0.0f;
            UpdateStatsText("Statistics cleared.");
        }

        IEnumerator RunAllRoutine()
        {
            for (var i = 0; i < m_Objects.Length; i++)
            {
                if (!IsReady(i))
                {
                    m_Busy = false;
                    yield break;
                }

                yield return RunCycleRoutine(i);
                yield return new WaitForSeconds(m_PauseBetweenCycles);
            }

            UpdateStatsText("All objects sorted.");
        }

        IEnumerator RunCycleRoutine(int index)
        {
            m_Busy = true;

            // try/finally guarantees that the busy flag is released even if the
            // cycle is stopped or something goes wrong half way through.
            try
            {
                var obj = m_Objects[index];
                var box = m_Boxes[index];
                var startTime = Time.time;

                UpdateStatsText($"Picking {obj.name} ...");

                m_TrajectoryPlanner.SetTargets(obj, box);
                m_TrajectoryPlanner.PublishJoints();

                // Phase 1: wait for the robot to start moving. If it never moves,
                // no trajectory was returned and the cycle has already failed.
                var moved = false;
                while (Time.time - startTime < m_StartGrace)
                {
                    if (IsRobotMoving())
                    {
                        moved = true;
                        break;
                    }

                    yield return new WaitForSeconds(0.1f);
                }

                if (!moved && m_Joints.Count > 0)
                {
                    Finish(obj, startTime, false, "no motion (planning failed?)");
                    yield break;
                }

                // Phase 2: run until the object is placed, or until the robot has
                // been standing still long enough for the cycle to be over.
                var lastMotion = Time.time;
                var placed = false;

                while (Time.time - startTime < m_CycleTimeout)
                {
                    if (IsPlaced(obj, box))
                    {
                        placed = true;
                        break;
                    }

                    if (IsRobotMoving())
                    {
                        lastMotion = Time.time;
                    }
                    else if (m_Joints.Count > 0 && Time.time - lastMotion > m_StillTime)
                    {
                        break;
                    }

                    yield return new WaitForSeconds(0.1f);
                }

                // Give the object a moment to settle, then check once more.
                if (!placed)
                {
                    yield return new WaitForSeconds(0.5f);
                    placed = IsPlaced(obj, box);
                }

                Finish(obj, startTime, placed, placed ? "placed" : "not placed");
            }
            finally
            {
                m_Busy = false;
            }
        }

        void Finish(GameObject obj, float startTime, bool success, string reason)
        {
            m_LastCycleTime = Time.time - startTime;
            m_Cycles++;
            m_TotalTime += m_LastCycleTime;

            if (success)
            {
                m_Successes++;
            }

            UpdateStatsText(success
                ? $"{obj.name}: placed in {m_LastCycleTime:F1} s."
                : $"{obj.name}: FAILED after {m_LastCycleTime:F1} s ({reason}).");
        }

        /// <summary>
        ///     True while any joint of the robot is still turning.
        /// </summary>
        bool IsRobotMoving()
        {
            foreach (var joint in m_Joints)
            {
                var velocity = joint.jointVelocity;
                for (var i = 0; i < velocity.dofCount; i++)
                {
                    if (Mathf.Abs(velocity[i]) > m_StillThreshold)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        ///     True when the object is close enough to its container, measured in the
        ///     horizontal plane so that the height of the container does not matter.
        /// </summary>
        bool IsPlaced(GameObject obj, GameObject box)
        {
            var a = obj.transform.position;
            var b = box.transform.position;
            a.y = 0.0f;
            b.y = 0.0f;

            return Vector3.Distance(a, b) < m_SuccessRadius;
        }

        /// <summary>
        ///     Checks that everything needed for a cycle is assigned.
        /// </summary>
        bool IsReady(int index)
        {
            if (m_TrajectoryPlanner == null)
            {
                Debug.LogError("PickAndPlaceManager: TrajectoryPlanner is not assigned.");
                return false;
            }

            if (m_Objects == null || m_Boxes == null || m_Objects.Length != m_Boxes.Length)
            {
                Debug.LogError("PickAndPlaceManager: Objects and Boxes must be assigned and have the same length.");
                return false;
            }

            if (index < 0 || index >= m_Objects.Length)
            {
                Debug.LogError($"PickAndPlaceManager: index {index} is out of range.");
                return false;
            }

            if (m_Objects[index] == null || m_Boxes[index] == null)
            {
                Debug.LogError($"PickAndPlaceManager: element {index} of Objects or Boxes is empty. " +
                               "Assign it in the Inspector.");
                return false;
            }

            return true;
        }

        void UpdateStatsText(string status)
        {
            if (m_StatsText == null)
            {
                return;
            }

            var failures = m_Cycles - m_Successes;
            var rate = m_Cycles > 0 ? 100.0f * m_Successes / m_Cycles : 0.0f;
            var average = m_Cycles > 0 ? m_TotalTime / m_Cycles : 0.0f;

            var text = new StringBuilder();
            text.AppendLine(status);
            text.AppendLine($"Cycles: {m_Cycles}   Success: {m_Successes}   Failed: {failures}");
            text.AppendLine($"Success rate: {rate:F0} %");
            text.AppendLine($"Last cycle: {m_LastCycleTime:F1} s   Average: {average:F1} s");

            m_StatsText.text = text.ToString();
        }
    }
}
