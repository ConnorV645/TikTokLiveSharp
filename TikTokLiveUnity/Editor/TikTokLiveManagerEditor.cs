﻿using System.Collections.Generic;
using System.Net.WebSockets;
using UnityEditor;
using UnityEngine;

namespace TikTokLiveUnity.Editor
{
    /// <summary>
    /// Inspector for TikTokLiveManager
    /// </summary>
    [CustomEditor(typeof(TikTokLiveManager))]
    public class TikTokLiveManagerEditor : UnityEditor.Editor
    {
        /// <summary>
        /// Holds Foldout-States for Inspectors
        /// </summary>
        private static Dictionary<Object, Dictionary<string, bool>> openFoldOuts;
        private static Dictionary<object, string> connectionHost = new Dictionary<object, string>();

        private static GUIStyle centeredText = null;

        /// <summary>
        /// Draws Inspector for Selected Object
        /// </summary>
        public override void OnInspectorGUI()
        {
            if (centeredText == null)
            {
                centeredText = new GUIStyle("Label");
                centeredText.alignment = TextAnchor.MiddleCenter;
            }
            if (openFoldOuts == null)
                openFoldOuts = new Dictionary<Object, Dictionary<string, bool>>();
            if (!openFoldOuts.ContainsKey(target))
                openFoldOuts.Add(target, new Dictionary<string, bool>());
            TikTokLiveManager mgr = (TikTokLiveManager)target;
            serializedObject.Update();
            bool allowChanges = true;
            if (Application.isPlaying && TikTokLiveManager.Exists && TikTokLiveManager.Instance == mgr)
                allowChanges = DrawStatus();
            EditorGUI.BeginChangeCheck();
            SerializedProperty hasRootProp = serializedObject.FindProperty("hasRootObject");
            if (mgr.transform.parent != null && !hasRootProp.boolValue)
                EditorGUILayout.HelpBox("Please set HasRootObject if your GameObject has a Parent", MessageType.Warning);
            EditorGUI.BeginDisabledGroup(Application.isPlaying && mgr.gameObject.scene.IsValid());
            EditorGUILayout.PropertyField(hasRootProp); // Has Root Object
            PropertyFieldWithLabel(serializedObject.FindProperty("texCacheSize"), "Cache Size"); // Texture Cache Size
            EditorGUILayout.PropertyField(serializedObject.FindProperty("autoConnect")); // Auto Connect
            PropertyFieldWithLabel(serializedObject.FindProperty("autoConnectHostId"), "Host Id"); // Auto Connect Host Id
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.Space(EditorGUIUtility.singleLineHeight * .5f);
            EditorGUI.BeginDisabledGroup(!allowChanges);
            PropertyFieldWithLabel(serializedObject.FindProperty("settings"), "Client Settings"); // Connection Settings
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.Space(EditorGUIUtility.singleLineHeight * .5f);
            DrawEvents();
            if (EditorGUI.EndChangeCheck())
                serializedObject.ApplyModifiedProperties();
        }

        /// <summary>
        /// Draws Status for TikTokLiveManager (During PlayMode)
        /// </summary>>
        private bool DrawStatus()
        {
            TikTokLiveManager mgr = (TikTokLiveManager)target;
            if (mgr.Connected)
            {
                GUILayout.Label(new GUIContent("Connected to:"), centeredText, GUILayout.ExpandWidth(true));
                GUILayout.Label(new GUIContent(mgr.HostName), new GUIStyle(centeredText) { fontStyle = FontStyle.Bold }, GUILayout.ExpandWidth(true));
                if (GUILayout.Button("Disconnect", GUILayout.ExpandWidth(true)))
                    mgr.DisconnectFromLivestreamAsync();
            }
            else if (mgr.Connecting)
            {
                GUILayout.Label(new GUIContent("Connecting"), centeredText, GUILayout.ExpandWidth(true));
                if (GUILayout.Button("Cancel", GUILayout.ExpandWidth(true)))
                    mgr.DisconnectFromLivestreamAsync();
            }
            else // Not Connected
            {
                GUILayout.Label(new GUIContent("Connect to:"), centeredText, GUILayout.ExpandWidth(true));
                if (!connectionHost.ContainsKey(mgr))
                    connectionHost.Add(mgr, serializedObject.FindProperty("autoConnectHostId").stringValue);
                connectionHost[mgr] = EditorGUILayout.TextField(connectionHost[mgr]);
                if (GUILayout.Button("Connect", GUILayout.ExpandWidth(true)))
                    mgr.ConnectToStreamAsync(connectionHost[mgr]);
            }
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
            return !mgr.Connected && !mgr.Connecting;
        }

        #region UI-Objects
        /// <summary>
        /// Draws PropertyField with adjusted Label
        /// </summary>
        /// <param name="property">Property to draw PropertyField for</param>
        /// <param name="label">Label to use for Field</param>
        private void PropertyFieldWithLabel(SerializedProperty property, string label)
        {
            GUIContent txtLabel = new GUIContent(label, property.tooltip);
            EditorGUILayout.PropertyField(property, txtLabel);
        }
        /// <summary>
        /// Draws a FoldOut
        /// </summary>
        /// <param name="path">Path for Foldout (for storing State)</param>
        /// <param name="label">Label for Foldout</param>
        /// <param name="defaultValue">Default Value for Open-Value</param>
        /// <returns>Whether FoldOut is Opened</returns>
        private bool DoFoldout(string path, GUIContent label, bool defaultValue = false)
        {
            Dictionary<string, bool> openStates = openFoldOuts[target];
            if (!openStates.ContainsKey(path))
                openStates.Add(path, defaultValue);
            openStates[path] = EditorGUILayout.Foldout(openStates[path], label, true);
            return openStates[path];
        }
        #endregion

        #region Events
        /// <summary>
        /// Draws All Events for TikTokLiveManager
        /// </summary>
        private void DrawEvents()
        {
            if (DoFoldout("Events", new GUIContent("Events", "Unity-Events on this Manager"), true))
            {
                EditorGUI.indentLevel++;
                DrawGenericEvents();
                DrawRoomEvents();
                DrawUserEvents();
                DrawHostEvents();
                DrawRankEvents();
                DrawLinkMicEvents();
                DrawMiscEvents();
                EditorGUI.indentLevel--;
            }
        }
        /// <summary>
        /// Draws Generic Events, Connection Events & Unhandled Events
        /// </summary>
        private void DrawGenericEvents()
        {
            if (DoFoldout("Events/Generic", new GUIContent("Generic Events")))
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("onException"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("onConnected"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("onDisconnected"));
            }
            if (DoFoldout("Events/Connection", new GUIContent("Connection Events")))
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("onLiveEnded"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("onLivePaused"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("onSystemMessage"));
            }
            if (DoFoldout("Events/Generic/Unhandled", new GUIContent("Unhandled Messages")))
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("onUnhandledEvent"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("onUnhandledSocialEvent"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("onUnhandledMemberEvent"));
            }
        }
        /// <summary>
        /// Draws RoomEvents
        /// </summary>
        private void DrawRoomEvents()
        {
            if (DoFoldout("Events/Room", new GUIContent("Room")))
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("onRoomIntro"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("onViewerData"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("onRoomMessage"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("onClosedCaption"));
            }
        }
        /// <summary>
        /// Draws UserEvents
        /// </summary>
        private void DrawUserEvents()
        {
            if (DoFoldout("Events/Viewer", new GUIContent("Viewer Events")))
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("onComment"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("onGift"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("onLike"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("onShare"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("onFollow"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("onJoin"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("onSubscribe"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("onGiftMessage"));
            }
        }
        /// <summary>
        /// Draws HostEvents
        /// </summary>
        private void DrawHostEvents()
        {
            if (DoFoldout("Events/Host", new GUIContent("Host Events")))
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("onPollMessage"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("onRoomPinMessage"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("onGoalUpdate"));
            }
        }
        /// <summary>
        /// Draws RankEvents
        /// </summary>
        private void DrawRankEvents()
        {
            if (DoFoldout("Events/Rank", new GUIContent("Ranking")))
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("onRankText"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("onRankUpdate"));
            }
        }
        /// <summary>
        /// Draws LinkMicEvents
        /// </summary>
        private void DrawLinkMicEvents()
        {
            if (DoFoldout("Events/LinkMic", new GUIContent("Link Mic Battle")))
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("onLinkMicBattle"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("onLinkMicArmies"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("onLinkMicMethod"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("onLinkMicFanTicket"));
            }
        }
        /// <summary>
        /// Draws Miscellaneous Events
        /// </summary>
        private void DrawMiscEvents()
        {
            if (DoFoldout("Events/Misc", new GUIContent("Miscellaneous")))
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("onInRoomBanner"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("onDetectMessage"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("onBarrageMessage"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("onUnauthorizedMember"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("onLinkMessage"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("onLinkLayerMessage"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("onGiftBroadcast"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("onShopMessage"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("onIMDelete"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("onQuestion"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("onEnvelope"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("onSubNotify"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("onEmote"));
            }
        }
        #endregion
    }
}
