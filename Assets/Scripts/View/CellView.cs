using UnityEngine;
using UnityEngine.EventSystems;

namespace ChessGame
{
    public class CellView : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer mainRenderer;
        [SerializeField] private Color highlightColor = new Color(0f, 1f, 0f, 0.5f);

        private Cell _cell;
        private bool _isHighlighted = false;

        // 单元格点击事件
        public delegate void CellClickedHandler(CellView cellView);
        public event CellClickedHandler OnCellClicked;

        private void Awake()
        {
            if (mainRenderer == null)
                mainRenderer = GetComponent<SpriteRenderer>();

            Debug.Log($"CellView Awake: {gameObject.name}");

            // 确保有碰撞体
            BoxCollider2D collider = GetComponent<BoxCollider2D>();
            if (collider == null)
            {
                Debug.LogWarning("CellView没有BoxCollider2D组件，添加一个");
                collider = gameObject.AddComponent<BoxCollider2D>();
            }

            // 设置碰撞体大小
            collider.size = new Vector2(1f, 1f);
        }

        // 初始化单元格视图
        public void Initialize(Cell cell)
        {
            _cell = cell;
            Debug.Log($"CellView Initialized: {cell.Position}");
        }

        // 切换高亮状态
        public void ToggleHighlight(bool isHighlighted)
        {
            _isHighlighted = isHighlighted;

            if (mainRenderer != null)
            {
                mainRenderer.color = _isHighlighted ? highlightColor : Color.white;
                Debug.Log($"CellView {(_isHighlighted ? "高亮" : "取消高亮")}: {_cell?.Position}");
            }
        }

        // 获取关联的数据模型
        public Cell GetCell()
        {
            return _cell;
        }

        // 处理鼠标点击
        private void OnMouseDown()
        {
            Debug.Log($"CellView.OnMouseDown: 位置 {_cell?.Position}");
            
            if (OnCellClicked != null && _cell != null)
            {
                Debug.Log($"触发OnCellClicked事件: {_cell.Position}");
                OnCellClicked(this);
            }
        }
    }
}