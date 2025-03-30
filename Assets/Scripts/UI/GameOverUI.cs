using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ChessGame
{
    public class GameOverUI : MonoBehaviour
    {
        [SerializeField] private GameObject gameOverPanel;
        [SerializeField] private TextMeshProUGUI resultText;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button mainMenuButton;
        
        private void Start()
        {
            // 隐藏游戏结束面板
            if (gameOverPanel != null)
                gameOverPanel.SetActive(false);
                
            // 订阅游戏结束事件
            if (GameEventSystem.Instance != null)
            {
                GameEventSystem.Instance.OnGameOver += ShowGameOverPanel;
                GameEventSystem.Instance.OnPlayerWon += ShowWinnerInfo;
            }
            
            // 设置按钮点击事件
            if (restartButton != null)
                restartButton.onClick.AddListener(RestartGame);
                
            if (mainMenuButton != null)
                mainMenuButton.onClick.AddListener(ReturnToMainMenu);
        }
        
        private void OnDestroy()
        {
            // 取消订阅事件
            if (GameEventSystem.Instance != null)
            {
                GameEventSystem.Instance.OnGameOver -= ShowGameOverPanel;
                GameEventSystem.Instance.OnPlayerWon -= ShowWinnerInfo;
            }
            
            // 移除按钮点击事件
            if (restartButton != null)
                restartButton.onClick.RemoveListener(RestartGame);
                
            if (mainMenuButton != null)
                mainMenuButton.onClick.RemoveListener(ReturnToMainMenu);
        }
        
        // 显示游戏结束面板
        private void ShowGameOverPanel()
        {
            if (gameOverPanel != null)
                gameOverPanel.SetActive(true);
        }
        
        // 显示胜利者信息
        private void ShowWinnerInfo(int winnerId)
        {
            if (resultText != null)
            {
                if (winnerId == 0)
                {
                    resultText.color = Color.green;
                    resultText.text = "玩家获胜！";
                }
                else
                {
                    resultText.color = Color.red;
                    resultText.text = "敌方获胜！";  
                }
            }
        }
        
        // 重新开始游戏
        private void RestartGame()
        {
            // 隐藏游戏结束面板
            if (gameOverPanel != null)
                gameOverPanel.SetActive(false);
                
            // 重置游戏
            GameController gameController = FindObjectOfType<GameController>();
            if (gameController != null)
                gameController.RestartGame();
                
            // 重置游戏结束检查器
            GameEndChecker gameEndChecker = FindObjectOfType<GameEndChecker>();
            if (gameEndChecker != null)
                gameEndChecker.ResetGame();
        }
        
        // 返回主菜单
        private void ReturnToMainMenu()
        {
            Debug.Log("返回主菜单");
            // 这里可以加载主菜单场景
            // SceneManager.LoadScene("MainMenu");
        }
    }
} 