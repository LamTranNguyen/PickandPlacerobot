using UnityEngine;

namespace Unity.Robotics.PickAndPlace
{
    /// <summary>
    ///     UC 5 - Bin picking and sorting.
    ///     Places the objects at random positions inside the bin area.
    ///     The bin is divided into one cell per object along the X axis, and each
    ///     object is placed randomly inside its own cell. This keeps the positions
    ///     random while guaranteeing that two objects never end up so close that the
    ///     gripper pushes a neighbouring object away while picking.
    /// </summary>
    public class BinRandomizer : MonoBehaviour
    {
        [Tooltip("The objects to randomize (same objects as in ObjectSelector).")]
        [SerializeField]
        GameObject[] m_Objects;

        [Tooltip("Half the width of the bin area along X, in meters.")]
        [SerializeField]
        float m_RangeX = 0.09f;

        [Tooltip("Half the depth of the bin area along Z, in meters.")]
        [SerializeField]
        float m_RangeZ = 0.02f;

        [Tooltip("Free space kept on both sides inside each cell, in meters.")]
        [SerializeField]
        float m_CellMargin = 0.015f;

        [Tooltip("Warn if the cells are narrower than this, in meters.")]
        [SerializeField]
        float m_MinDistance = 0.06f;

        [Tooltip("Shuffle which object goes into which cell.")]
        [SerializeField]
        bool m_ShuffleCells = true;

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

            var cellWidth = 2.0f * m_RangeX / m_Objects.Length;
            if (cellWidth < m_MinDistance)
            {
                Debug.LogWarning(
                    $"BinRandomizer: cell width is {cellWidth:F3} m but the minimum distance is " +
                    $"{m_MinDistance:F3} m. Increase Range X or use fewer objects.");
            }
        }

        /// <summary>
        ///     Places every object at a new random position inside its own cell.
        ///     Call this from a UI button.
        /// </summary>
        public void Randomize()
        {
            if (!m_Initialized)
            {
                Debug.LogWarning("BinRandomizer: not initialized yet.");
                return;
            }

            var cells = BuildCellOrder(m_Objects.Length);
            var cellWidth = 2.0f * m_RangeX / m_Objects.Length;

            // Room left inside a cell once the margins are removed.
            var jitterX = Mathf.Max(0.0f, cellWidth * 0.5f - m_CellMargin);

            for (var i = 0; i < m_Objects.Length; i++)
            {
                // Centre of this object's cell, measured from the left edge of the area.
                var cellCenterX = m_AreaCenter.x - m_RangeX + cellWidth * (cells[i] + 0.5f);

                var position = new Vector3(
                    cellCenterX + Random.Range(-jitterX, jitterX),
                    m_Height,
                    m_AreaCenter.z + Random.Range(-m_RangeZ, m_RangeZ));

                PlaceObject(m_Objects[i], position);
            }

            Debug.Log("BinRandomizer: objects placed at new random positions.");
        }

        /// <summary>
        ///     Returns the cell index assigned to each object, shuffled if requested.
        /// </summary>
        int[] BuildCellOrder(int count)
        {
            var cells = new int[count];
            for (var i = 0; i < count; i++)
            {
                cells[i] = i;
            }

            if (!m_ShuffleCells)
            {
                return cells;
            }

            // Fisher-Yates shuffle.
            for (var i = count - 1; i > 0; i--)
            {
                var j = Random.Range(0, i + 1);
                var temp = cells[i];
                cells[i] = cells[j];
                cells[j] = temp;
            }

            return cells;
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
