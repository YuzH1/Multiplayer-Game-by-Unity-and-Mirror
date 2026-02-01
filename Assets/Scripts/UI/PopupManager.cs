using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections.Generic;

namespace MultiplayerGame.UI
{
    /// <summary>
    /// 全局弹窗管理器（单例模式）
    /// 使用方式：PopupManager.Instance.Show("标题", "内容", onConfirm, onCancel);
    /// </summary>
    public class PopupManager : MonoBehaviour
    {
        public static PopupManager Instance { get; private set; }

        [Header("Popup UI References")]
        [SerializeField] private GameObject popupPanel;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text messageText;
        [SerializeField] private Button confirmButton;
        [SerializeField] private Button cancelButton;
        [SerializeField] private TMP_Text confirmButtonText;
        [SerializeField] private TMP_Text cancelButtonText;

        [Header("Settings")]
        [SerializeField] private bool dontDestroyOnLoad = true;

        private Action onConfirmCallback;
        private Action onCancelCallback;
        private Queue<PopupData> popupQueue = new Queue<PopupData>();
        private bool isShowing = false;

        private struct PopupData
        {
            public string title;
            public string message;
            public string confirmText;
            public string cancelText;
            public Action onConfirm;
            public Action onCancel;
            public bool showCancel;
        }

        void Awake()
        {
            // 单例设置
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            if (dontDestroyOnLoad)
            {
                DontDestroyOnLoad(gameObject);
            }

            // 初始化按钮事件
            if (confirmButton != null)
            {
                confirmButton.onClick.AddListener(OnConfirmClicked);
            }

            if (cancelButton != null)
            {
                cancelButton.onClick.AddListener(OnCancelClicked);
            }

            // 初始隐藏弹窗
            Hide();
        }

        void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        #region Public API

        /// <summary>
        /// 显示确认弹窗（只有确定按钮）
        /// </summary>
        /// <param name="title">标题</param>
        /// <param name="message">消息内容</param>
        /// <param name="onConfirm">确认回调</param>
        /// <param name="confirmText">确认按钮文字，默认为"确定"</param>
        public void ShowConfirm(string title, string message, Action onConfirm = null, string confirmText = "确定")
        {
            EnqueuePopup(new PopupData
            {
                title = title,
                message = message,
                confirmText = confirmText,
                cancelText = "",
                onConfirm = onConfirm,
                onCancel = null,
                showCancel = false
            });
        }

        /// <summary>
        /// 显示带确认和取消按钮的弹窗
        /// </summary>
        /// <param name="title">标题</param>
        /// <param name="message">消息内容</param>
        /// <param name="onConfirm">确认回调</param>
        /// <param name="onCancel">取消回调</param>
        /// <param name="confirmText">确认按钮文字，默认为"确定"</param>
        /// <param name="cancelText">取消按钮文字，默认为"取消"</param>
        public void ShowChoice(string title, string message, Action onConfirm, Action onCancel = null, 
            string confirmText = "确定", string cancelText = "取消")
        {
            EnqueuePopup(new PopupData
            {
                title = title,
                message = message,
                confirmText = confirmText,
                cancelText = cancelText,
                onConfirm = onConfirm,
                onCancel = onCancel,
                showCancel = true
            });
        }

        /// <summary>
        /// 显示简单消息（只有确定按钮，无回调）
        /// </summary>
        /// <param name="title">标题</param>
        /// <param name="message">消息内容</param>
        public void ShowMessage(string title, string message)
        {
            ShowConfirm(title, message, null, "确定");
        }

        /// <summary>
        /// 显示错误消息
        /// </summary>
        /// <param name="message">错误内容</param>
        public void ShowError(string message)
        {
            ShowConfirm("错误", message, null, "确定");
        }

        /// <summary>
        /// 显示警告消息
        /// </summary>
        /// <param name="message">警告内容</param>
        public void ShowWarning(string message)
        {
            ShowConfirm("警告", message, null, "确定");
        }

        /// <summary>
        /// 显示成功消息
        /// </summary>
        /// <param name="message">成功内容</param>
        public void ShowSuccess(string message)
        {
            ShowConfirm("成功", message, null, "确定");
        }

        /// <summary>
        /// 强制隐藏当前弹窗
        /// </summary>
        public void ForceHide()
        {
            Hide();
            popupQueue.Clear();
        }

        /// <summary>
        /// 检查是否有弹窗正在显示
        /// </summary>
        public bool IsShowing => isShowing;

        #endregion

        #region Private Methods

        private void EnqueuePopup(PopupData data)
        {
            popupQueue.Enqueue(data);

            if (!isShowing)
            {
                ShowNextPopup();
            }
        }

        private void ShowNextPopup()
        {
            if (popupQueue.Count == 0)
            {
                isShowing = false;
                return;
            }

            var data = popupQueue.Dequeue();
            ShowInternal(data);
        }

        private void ShowInternal(PopupData data)
        {
            if (popupPanel == null)
            {
                Debug.LogWarning("[PopupManager] 弹窗面板未配置，直接执行回调");
                data.onConfirm?.Invoke();
                ShowNextPopup();
                return;
            }

            isShowing = true;

            // 设置文本
            if (titleText != null)
                titleText.text = data.title;

            if (messageText != null)
                messageText.text = data.message;

            if (confirmButtonText != null)
                confirmButtonText.text = data.confirmText;

            if (cancelButtonText != null)
                cancelButtonText.text = data.cancelText;

            // 显示/隐藏取消按钮
            if (cancelButton != null)
                cancelButton.gameObject.SetActive(data.showCancel);

            // 存储回调
            onConfirmCallback = data.onConfirm;
            onCancelCallback = data.onCancel;

            // 显示弹窗
            popupPanel.SetActive(true);
        }

        private void Hide()
        {
            if (popupPanel != null)
                popupPanel.SetActive(false);

            onConfirmCallback = null;
            onCancelCallback = null;
            isShowing = false;
        }

        private void OnConfirmClicked()
        {
            var callback = onConfirmCallback;
            Hide();
            callback?.Invoke();
            ShowNextPopup();
        }

        private void OnCancelClicked()
        {
            var callback = onCancelCallback;
            Hide();
            callback?.Invoke();
            ShowNextPopup();
        }

        #endregion
    }
}
