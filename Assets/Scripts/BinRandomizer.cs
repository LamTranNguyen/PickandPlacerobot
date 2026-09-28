using UnityEngine;

namespace Unity.Robotics.PickAndPlace
{
    /// <summary>
    ///     UC 5 - Bin picking and sorting.
    ///     Places the objects at random positions inside the bin area.
    ///     The bin area is defined relative to the position each object had
    ///     when the scene started, so no extra setup is needed.
    /// </summary>
    public class BinRandomizer : MonoBehaviour
    {
        [Tooltip("The objects to randomize (same objects as in ObjectSelector).")]
        [SerializeField]
        GameObject[] m_Objects;

        [Tooltip("Half the size of the bin area along X, in meters.")]
        [SerializeField]
        float m_RangeX = 0.05f;

        [Tooltip("Half the size of the bin area along Z, in meters.")]
        [SerializeField]
        float m_RangeZ = 0.04f;

        [Tooltip("Minimum distance between two objects, in meters.")]
        [SerializeField]
        float m_MinDistance = 0.05f;

        [Tooltip("Randomize the rotation of the objects around the vertical axis.")]
        [SerializeField]
        bool m_RandomizeRotation;

        Vector3 m_AreaCenter;
        float m_Height;
        bool m_Initialized;

        void Start()
        {
            if (m_Objects == null || m_Objects.Length == 0)
            {
                Debug.LogError("BinRandomizer: no objects assigned.");
                return;
            }

            // The centre of the bin area is the average of the starting positions.
            var sum = Vector3.zero;
            foreach (var obj in m_Objects)
            {
                sum += obj.transform.position;
            }

            m_AreaCenter = sum / m_Objects.Length;
            m_Height = m_Objects[0].transform.position.y;
            m_Initialized = true;
        }

        /// <summary>
        ///     Places every object at a new random position inside the bin area.
        ///     Call this from a UI button.
        /// </summary>
        public void Randomize()
        {
            if (!m_Initialized)
            {
                Debug.LogWarning("BinRandomizer: not initialized yet.");
                return;
            }

            var placed = new Vector3[m_Objects.Length];

            for (var i = 0; i < m_Objects.Length; i++)
            {
                var position = Vector3.zero;

                // Try a few times to find a spot that is not too close to the others.
                for (var attempt = 0; attempt < 30; attempt++)
                {
                    position = new Vector3(
                        m_AreaCenter.x + Random.Range(-m_RangeX, m_RangeX),
                        m_Height,
                        m_AreaCenter.z + Random.Range(-m_RangeZ, m_RangeZ));

                    var tooClose = false;
                    for (var j = 0; j < i; j++)
                    {
                        if (Vector3.Distance(position, placed[j]) < m_MinDistance)
                        {
                            tooClose = true;
                            break;
                        }
                    }

                    if (!tooClose)
                    {
                        break;
                    }
                }

                placed[i] = position;
                PlaceObject(m_Objects[i], position);
            }

            Debug.Log("BinRandomizer: objects placed at new random positions.");
        }

        void PlaceObject(GameObject obj, Vector3 position)
        {
            // Stop any motion left over from the previous run.
            var body = obj.GetComponent<Rigidbody>();
            if (body != null)
            {
                body.velocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
            }

            obj.transform.position = position;
            obj.transform.rotation = m_RandomizeRotation
                ? Quaternion.Euler(0.0f, Random.Range(0.0f, 360.0f), 0.0f)
                : Quaternion.identity;
        }
    }
}
