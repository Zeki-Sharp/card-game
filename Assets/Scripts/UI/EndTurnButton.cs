using UnityEngine;
using UnityEngine.UI;

namespace ChessGame
{
    public class EndTurnButton : MonoBehaviour
    {
        private Button _button;
        private TurnManager _turnManager;
        
        private void Awake()
        {
            _button = GetComponent<Button>();
            if (_button == null)
            {
                Debug.LogError("EndTurnButton需要Button组件");
                return;
            }
            
            _turnManager = FindObjectOfType<TurnManager>();
            if (_turnManager == null)
            {
                Debug.LogError("找不到TurnManager组件");
                return;
            }

            // 订阅游戏结束事件
            if (GameEventSystem.Instance != null)
            {
                GameEventSystem.Instance.OnGameOver += CloseSelf;
            }
            
            // 添加点击事件
            _button.onClick.AddListener(OnButtonClick);
        }
        
        private void OnDestroy()
        {
            if (_button != null)
            {
                _button.onClick.RemoveListener(OnButtonClick);
            }

            // 取消订阅游戏结束事件
            if (GameEventSystem.Instance != null)
            {
                GameEventSystem.Instance.OnGameOver -= CloseSelf;
            }
        }
        
        private void Update()
        {
            // 只在玩家回合启用按钮
            if (_turnManager != null)
            {
                _button.interactable = _turnManager.IsPlayerTurn();
            }
        }
        
        private void OnButtonClick()
        {
            if (_turnManager != null && _turnManager.IsPlayerTurn())
            {
                Debug.Log("点击结束回合按钮");
                _turnManager.EndPlayerTurn();
            }
        }

        //关闭自身
        private void CloseSelf()
        {
            gameObject.SetActive(false);
        }
    }
} 