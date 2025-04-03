using UnityEngine;
using UnityEngine.EventSystems;

namespace ChessGame
{
    public class CellView : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer mainRenderer;
        [SerializeField] private Color moveHighlightColor = new Color(0f, 1f, 0f, 0.5f);
        [SerializeField] private Color selectedHighlightColor = new Color(1f, 1f, 0f, 0.5f);
        [SerializeField] private Color attackHighlightColor = new Color(1f, 0f, 0f, 0.5f);

        private Cell _cell;
        private HighlightType _highlightType = HighlightType.None;

        // 高亮类型枚举
        public enum HighlightType
        {
            None,
            Move,
            Attack,
            Selected
        }

        // 单元格点击事件
        public delegate void CellClickedHandler(CellView cellView);
        public event CellClickedHandler OnCellClicked;

        private void Awake()
        {
            if (mainRenderer == null)
                mainRenderer = GetComponent<SpriteRenderer>();

            Debug.Log($"CellView Awake: {gameObject.name}");

            // 确保有碰撞体
            BoxCollider collider = GetComponent<BoxCollider>();
            if (collider == null)
            {
                Debug.LogWarning("CellView没有BoxCollider组件，添加一个");
                collider = gameObject.AddComponent<BoxCollider>();
            }

            // 设置碰撞体大小
            collider.size = new Vector3(1f, 0.1f, 1f); // 调整Y轴高度使其更薄
            collider.center = new Vector3(0f, 0f, 0f); // 调整中心点
        }

        // 初始化单元格视图
        public void Initialize(Cell cell)
        {
            _cell = cell;
            Debug.Log($"CellView Initialized: {cell.Position}");
        }

        // 设置高亮类型
        public void SetHighlight(HighlightType type)
        {
            _highlightType = type;

            if (mainRenderer != null)
            {
                switch (type)
                {
                    case HighlightType.None:
                        mainRenderer.color = Color.white;
                        break;
                    case HighlightType.Move:
                        mainRenderer.color = moveHighlightColor;
                        break;
                    case HighlightType.Selected:
                        mainRenderer.color = selectedHighlightColor;
                        break;
                    case HighlightType.Attack:
                        mainRenderer.color = attackHighlightColor;
                        break;
                }
                
                Debug.Log($"CellView 高亮类型 {_highlightType}: {_cell?.Position}");
            }
        }

        // 获取关联的数据模型
        public Cell GetCell()
        {
            return _cell;
        }

        // 为了兼容现有代码，保留原来的ToggleHighlight方法
        public void ToggleHighlight(bool isHighlighted)
        {
            SetHighlight(isHighlighted ? HighlightType.Move : HighlightType.None);
        }
    }
}