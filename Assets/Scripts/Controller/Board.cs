using UnityEngine;

namespace ChessGame
{
    public class Board : MonoBehaviour
    {
        [SerializeField] private int width = 4;
        [SerializeField] private int height = 6;
        [SerializeField] private float cellSize = 1f;
        [SerializeField] private GameObject cellPrefab;
        
        // 棋盘数据
        private Cell[,] _cells;
        
        // 棋盘视图
        private CellView[,] _cellViews;
        
        // 单元格点击事件，转发给GameController
        public delegate void CellClickedHandler(CellView cellView);
        public event CellClickedHandler OnCellClicked;
        
        // 添加公共属性以访问棋盘尺寸
        public int Width => width;
        public int Height => height;
        
        // 在Board.cs中添加初始化完成事件
        public event System.Action OnBoardInitialized;
        
        // 在Board.cs中添加初始化状态属性
        public bool IsInitialized { get; private set; } = false;
        
        // 初始化
        private void Awake()
        {
            Debug.Log("Board.Awake: 开始初始化棋盘");
            InitializeBoard();
        }
        
        // 初始化棋盘
        public void InitializeBoard()
        {
            // 如果已经初始化，直接返回
            if (IsInitialized)
            {
                Debug.Log("Board 已经初始化，跳过重复初始化");
                return;
            }
            
            Debug.Log("开始初始化棋盘");
            
            // 创建数据模型
            _cells = new Cell[width, height];
            _cellViews = new CellView[width, height];
            
            // 计算棋盘中心偏移
            Vector3 boardOffset = new Vector3(
                -((width - 1) * cellSize) / 2f,
                -((height - 1) * cellSize) / 2f,
                0f
            );
            
            // 创建单元格
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    // 创建单元格数据
                    _cells[x, y] = new Cell(new Vector2Int(x, y));
                    
                    // 创建单元格视图
                    Vector3 position = new Vector3(x * cellSize, y * cellSize, 0f) + boardOffset;
                    GameObject cellObject = Instantiate(cellPrefab, position, Quaternion.identity, transform);
                    cellObject.name = $"Cell_{x}_{y}";
                    
                    // 获取并初始化单元格视图组件
                    CellView cellView = cellObject.GetComponent<CellView>();
                    if (cellView != null)
                    {
                        cellView.Initialize(_cells[x, y]);
                        _cellViews[x, y] = cellView;
                        
                        // 注册点击事件，转发给GameController
                        cellView.OnCellClicked += ForwardCellClicked;
                    }
                }
            }
            
            // 标记为已初始化
            IsInitialized = true;
            
            // 初始化完成后触发事件
            OnBoardInitialized?.Invoke();
            
            Debug.Log("棋盘初始化完成");
        }
        
        // 转发单元格点击事件
        private void ForwardCellClicked(CellView cellView)
        {
            Debug.Log($"Board 接收到单元格点击: {cellView.GetCell().Position}");
            
            if (OnCellClicked != null)
            {
                Debug.Log("Board 转发点击事件到GameController");
                OnCellClicked.Invoke(cellView);
            }
            else
            {
                Debug.LogWarning("没有监听器注册到Board的OnCellClicked事件");
            }
        }
        
        // 获取指定位置的单元格
        public Cell GetCell(int x, int y)
        {
            if (x >= 0 && x < width && y >= 0 && y < height)
            {
                return _cells[x, y];
            }
            return null;
        }
        
        // 获取指定位置的单元格视图
        public CellView GetCellView(int x, int y)
        {
            if(_cellViews == null)
            {
                Debug.LogError("Board.GetCellView为空， 单元格视图数组未初始化");
                return null;
            }
            if (x >= 0 && x < width && y >= 0 && y < height)
            {
                return _cellViews[x, y];
            }
            return null;
        }
    }
} 