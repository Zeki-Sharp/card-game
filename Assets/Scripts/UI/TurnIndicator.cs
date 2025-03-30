using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ChessGame
{
    public class TurnIndicator : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI turnText;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Color playerTurnColor = new Color(0.2f, 0.6f, 1f);
        [SerializeField] private Color enemyTurnColor = new Color(1f, 0.3f, 0.3f);
        
        private TurnManager _turnManager;
        
        private void Start()
        {
            _turnManager = FindObjectOfType<TurnManager>();
            
            if (_turnManager != null)
            {
                // 订阅回合变化事件
                _turnManager.OnTurnChanged += OnTurnChanged;
                
                // 初始化显示
                OnTurnChanged(_turnManager.CurrentTurn);
            }
            else
            {
                Debug.LogError("找不到TurnManager组件");
            }

            // 订阅游戏结束事件
            if (GameEventSystem.Instance != null)
            {
                GameEventSystem.Instance.OnGameOver += CloseSelf;
            }
        }
        
        private void OnDestroy()
        {
            // 取消订阅事件
            if (_turnManager != null)
            {
                _turnManager.OnTurnChanged -= OnTurnChanged;
            }

            // 取消订阅游戏结束事件
            if (GameEventSystem.Instance != null)
            {
                GameEventSystem.Instance.OnGameOver -= CloseSelf;
            }
        }
        
        // 处理回合变化
        private void OnTurnChanged(TurnState turnState)
        {
            if (turnText != null)
            {
                turnText.text = turnState == TurnState.PlayerTurn ? "玩家回合" : "敌方回合";
            }
            
            if (backgroundImage != null)
            {
                backgroundImage.color = turnState == TurnState.PlayerTurn ? playerTurnColor : enemyTurnColor;
            }
        }

        //关闭自身
        private void CloseSelf()
        {
            gameObject.SetActive(false);
        }
    }
} 