using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace HideTheKing.Core
{
    public class GameModesExplained : MonoBehaviour
    {
        [SerializeField] private Canvas targetCanvas;
        [SerializeField] private List<RectTransform> images = new List<RectTransform>(4);
        [SerializeField, Range(0f, 1f)] private float alphaClickThreshold = 0.1f;

        private void Awake()
        {
            if (targetCanvas != null)
            {
                targetCanvas.gameObject.SetActive(true);
            }

            RegisterClickHandlers();
        }

        private void RegisterClickHandlers()
        {
            foreach (RectTransform imageRect in images)
            {
                if (imageRect == null)
                {
                    continue;
                }

                BringToFrontOnClick clickHandler = imageRect.GetComponent<BringToFrontOnClick>();
                if (clickHandler == null)
                {
                    clickHandler = imageRect.gameObject.AddComponent<BringToFrontOnClick>();
                }

                Image image = imageRect.GetComponent<Image>();
                if (image != null)
                {
                    image.raycastTarget = true;
                    TryConfigureAlphaHitTest(image);
                }

                clickHandler.Initialize(imageRect);
            }
        }

        private void TryConfigureAlphaHitTest(Image image)
        {
            if (image.sprite == null || image.sprite.texture == null)
            {
                return;
            }

            Texture2D texture = image.sprite.texture;
            if (!texture.isReadable)
            {
                Debug.LogWarning($"Skipping alpha hit test on '{image.name}' because texture '{texture.name}' is not readable.", image);
                return;
            }

            try
            {
                image.alphaHitTestMinimumThreshold = alphaClickThreshold;
            }
            catch (System.InvalidOperationException)
            {
                Debug.LogWarning($"Skipping alpha hit test on '{image.name}'. Texture '{texture.name}' may use Crunch compression.", image);
            }
        }
        
        public void OnExitCanvasButtonClicked()
        {
            if (targetCanvas != null)
            {
                GameObject canvasObject = targetCanvas.gameObject;
                canvasObject.SetActive(!canvasObject.activeSelf);
            }
            
        }
        
    }

    public class BringToFrontOnClick : MonoBehaviour, IPointerClickHandler
    {
        private RectTransform target;

        public void Initialize(RectTransform targetRect)
        {
            target = targetRect;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
            {
                return;
            }

            if (target != null)
            {
                target.SetAsLastSibling();
            }
        }
    }
    
    
}