using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;

namespace ChessGame
{
    public class CellView : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer mainRenderer;
        [SerializeField] private Color moveHighlightColor = new Color(0f, 1f, 0f, 0.5f);
        [SerializeField] private Color selectedHighlightColor = new Color(1f, 1f, 0f, 0.5f);
        [SerializeField] private Color attackHighlightColor = new Color(1f, 0f, 0f, 0.5f);
        [SerializeField] private Color abilityHighlightColor = new Color(0f, 0.5f, 1f, 0.5f);
        [SerializeField] private float highlightAnimationDuration = 0.3f;

        private Cell _cell;
        private HighlightType _highlightType = HighlightType.None;
        private Coroutine _activeAnimationCoroutine;
        private Color _originalColor;

        // 高亮类型枚举
        public enum HighlightType
        {
            None,
            Move,
            Attack,
            Selected,
            Ability
        }

        // 单元格点击事件
        public delegate void CellClickedHandler(CellView cellView);
        public event CellClickedHandler OnCellClicked;

        private void Awake()
        {
            if (mainRenderer == null)
                mainRenderer = GetComponent<SpriteRenderer>();
            
            _originalColor = mainRenderer.color;

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
            // 如果类型没有变化，不做任何事
            if (_highlightType == type) return;
            
            _highlightType = type;

            // 停止当前正在进行的动画
            if (_activeAnimationCoroutine != null)
            {
                StopCoroutine(_activeAnimationCoroutine);
                _activeAnimationCoroutine = null;
            }

            // 开始新的动画
            if (mainRenderer != null)
            {
                Color targetColor = _originalColor;
                
                switch (type)
                {
                    case HighlightType.None:
                        targetColor = _originalColor;
                        break;
                    case HighlightType.Move:
                        targetColor = moveHighlightColor;
                        break;
                    case HighlightType.Selected:
                        targetColor = selectedHighlightColor;
                        break;
                    case HighlightType.Attack:
                        targetColor = attackHighlightColor;
                        break;
                    case HighlightType.Ability:
                        targetColor = abilityHighlightColor;
                        break;
                }
                
                // 开始渐变动画
                _activeAnimationCoroutine = StartCoroutine(AnimateColorChange(targetColor));
                
                Debug.Log($"CellView 高亮类型 {_highlightType}: {_cell?.Position}");
            }
        }
        
        // 渐变动画协程
        private IEnumerator AnimateColorChange(Color targetColor)
        {
            Color startColor = mainRenderer.color;
            float elapsed = 0f;
            
            while (elapsed < highlightAnimationDuration)
            {
                mainRenderer.color = Color.Lerp(startColor, targetColor, elapsed / highlightAnimationDuration);
                elapsed += Time.deltaTime;
                yield return null;
            }
            
            // 确保颜色最终正确
            mainRenderer.color = targetColor;
            _activeAnimationCoroutine = null;
        }
        
        // 播放呼吸效果
        public void PlayPulseEffect(float duration = 2.0f)
        {
            // 停止当前正在进行的动画
            if (_activeAnimationCoroutine != null)
            {
                StopCoroutine(_activeAnimationCoroutine);
            }
            
            _activeAnimationCoroutine = StartCoroutine(PulseEffectCoroutine(duration));
        }
        
        // 呼吸效果协程
        private IEnumerator PulseEffectCoroutine(float duration)
        {
            float elapsed = 0f;
            Color currentColor = mainRenderer.color;
            Color pulseColor = new Color(currentColor.r, currentColor.g, currentColor.b, 0.7f);
            
            // 循环呼吸效果直到持续时间结束
            while (elapsed < duration)
            {
                // 使用正弦函数创建平滑的呼吸效果
                float t = (Mathf.Sin(elapsed * 4f) + 1f) / 2f;
                mainRenderer.color = Color.Lerp(currentColor, pulseColor, t);
                
                elapsed += Time.deltaTime;
                yield return null;
            }
            
            // 恢复原始颜色
            mainRenderer.color = currentColor;
            _activeAnimationCoroutine = null;
        }
        
        // 播放闪烁效果
        public void PlayBlinkEffect(Color blinkColor, int blinkCount = 3, float blinkSpeed = 0.2f)
        {
            // 停止当前正在进行的动画
            if (_activeAnimationCoroutine != null)
            {
                StopCoroutine(_activeAnimationCoroutine);
            }
            
            _activeAnimationCoroutine = StartCoroutine(BlinkEffectCoroutine(blinkColor, blinkCount, blinkSpeed));
        }
        
        // 闪烁效果协程
        private IEnumerator BlinkEffectCoroutine(Color blinkColor, int blinkCount, float blinkSpeed)
        {
            Color originalColor = mainRenderer.color;
            
            for (int i = 0; i < blinkCount; i++)
            {
                // 切换到闪烁颜色
                mainRenderer.color = blinkColor;
                yield return new WaitForSeconds(blinkSpeed);
                
                // 恢复原始颜色
                mainRenderer.color = originalColor;
                yield return new WaitForSeconds(blinkSpeed);
            }
            
            _activeAnimationCoroutine = null;
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