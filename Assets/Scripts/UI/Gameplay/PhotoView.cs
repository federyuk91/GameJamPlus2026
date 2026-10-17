using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace GameJamPlus.UI
{
    public sealed class PhotoView : IDisposable
    {
        private readonly VisualElement overlay;
        private readonly VisualElement photoArea;
        private readonly Image image;
        private readonly Label placeholder;
        private readonly Button close;
        private readonly Action closeRequested;

        public bool IsVisible => overlay.style.display.value == DisplayStyle.Flex;

        public PhotoView(VisualElement root, Action closeRequested)
        {
            this.closeRequested = closeRequested;
            overlay = root.Q("photo-overlay");
            photoArea = root.Q(className: "photo-content");
            image = root.Q<Image>("collected-photo");
            placeholder = root.Q<Label>("photo-placeholder");
            close = root.Q<Button>("photo-close");
            overlay.pickingMode = PickingMode.Position;
            close.pickingMode = PickingMode.Position;
            close.clicked += RequestClose;
            image.scaleMode = ScaleMode.ScaleToFit;
            photoArea.RegisterCallback<GeometryChangedEvent>(OnPhotoAreaChanged);
            Hide();
        }

        public void SetCloseKey(string key)
        {
            close.text = key + " · Next";
        }

        public void Show(Texture2D photo)
        {
            image.image = photo;
            image.style.display = photo != null ? DisplayStyle.Flex : DisplayStyle.None;
            placeholder.style.display = photo == null ? DisplayStyle.Flex : DisplayStyle.None;
            overlay.style.display = DisplayStyle.Flex;
            FitPhoto();
        }

        public void Hide()
        {
            overlay.style.display = DisplayStyle.None;
            image.image = null;
        }

        private void RequestClose() => closeRequested?.Invoke();

        private void OnPhotoAreaChanged(GeometryChangedEvent geometryEvent) => FitPhoto();

        private void FitPhoto()
        {
            Texture photo = image.image;
            Rect available = photoArea.contentRect;
            if (photo == null || available.width <= 0f || available.height <= 0f) return;

            float scale = Mathf.Min(available.width / photo.width, available.height / photo.height);
            float width = photo.width * scale;
            float height = photo.height * scale;
            image.style.width = width;
            image.style.height = height;
            image.style.left = (available.width - width) * 0.5f;
            image.style.top = (available.height - height) * 0.5f;
        }

        public void Dispose()
        {
            close.clicked -= RequestClose;
            photoArea.UnregisterCallback<GeometryChangedEvent>(OnPhotoAreaChanged);
        }
    }
}
