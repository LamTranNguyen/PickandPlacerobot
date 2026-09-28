using UnityEngine;
using UnityEngine.UI;

namespace Unity.Robotics.PickAndPlace
{
    /// <summary>
    ///     UC 5 - Bin picking and sorting.
    ///     Lets the user choose which object the robot should pick.
    ///     The chosen object and its matching sorting container are passed
    ///     to the TrajectoryPlanner before the trajectory is requested.
    /// </summary>
    public class ObjectSelector : MonoBehaviour
    {
        [SerializeField]
        TrajectoryPlanner m_TrajectoryPlanner;

        [SerializeField]
        Dropdown m_Dropdown;

        [Tooltip("Objects in the bin, in the same order as the dropdown options.")]
        [SerializeField]
        GameObject[] m_Objects;

        [Tooltip("Sorting container for each object, in the same order.")]
        [SerializeField]
        GameObject[] m_Boxes;

        void Start()
        {
            if (m_TrajectoryPlanner == null)
            {
                Debug.LogError("ObjectSelector: TrajectoryPlanner is not assigned.");
                return;
            }

            if (m_Objects.Length != m_Boxes.Length)
            {
                Debug.LogError("ObjectSelector: Objects and Boxes must have the same length.");
                return;
            }

            if (m_Dropdown != null)
            {
                m_Dropdown.onValueChanged.AddListener(SelectObject);
                SelectObject(m_Dropdown.value);
            }
        }

        /// <summary>
        ///     Assigns the object at the given index, and its matching container,
        ///     to the TrajectoryPlanner.
        /// </summary>
        public void SelectObject(int index)
        {
            if (index < 0 || index >= m_Objects.Length)
            {
                Debug.LogWarning($"ObjectSelector: index {index} is out of range.");
                return;
            }

            var selectedObject = m_Objects[index];
            var selectedBox = m_Boxes[index];

            m_TrajectoryPlanner.SetTargets(selectedObject, selectedBox);

            Debug.Log($"Selected: {selectedObject.name} -> {selectedBox.name}");
        }
    }
}
