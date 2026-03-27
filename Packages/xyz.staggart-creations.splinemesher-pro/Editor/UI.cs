using System;
using sc.splinemesher.pro.runtime;
using UnityEditor;
using UnityEditor.AnimatedValues;
using UnityEditor.EditorTools;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace sc.splinemesher.pro.editor
{
    public static class UI
    {
        public static readonly Color RedColor = new Color(1f, 0.31f, 0.34f);
        public static readonly Color OrangeColor= new Color(1f, 0.68f, 0f);
        public static readonly Color GreenColor = new Color(0.33f, 1f, 0f);
        public static readonly Color LightBlueColor = new Color(0.4f, 0.6f, 1f); 
        public static readonly Color LighterBlueColor = new Color(0.6f, 0.8f, 1f); 
        
        private const float HeaderHeight = 23f;

        public static void DrawHeader()
        {
            Rect rect = EditorGUILayout.GetControlRect();

            //Draw title
            GUIContent textContent = new GUIContent($"{AssetInfo.ASSET_NAME}");
            Vector2 titleSize = EditorStyles.boldLabel.CalcSize(textContent);
            
            Rect textRect = new Rect(rect.x + 10f, rect.y, titleSize.x, titleSize.y);
            GUI.Label(textRect, textContent, EditorStyles.boldLabel);
            float totalWidth = textRect.width + 13f;
            
            //Draw icon to the right
            Rect iconRect = new Rect(rect.x + totalWidth, rect.y + 2f, Icons.Edition.width, Icons.Edition.height);
            GUI.DrawTexture(iconRect, Icons.Edition, ScaleMode.ScaleToFit);
            totalWidth += iconRect.width + 2f;
            
            //Version
            GUIContent version = new GUIContent($"{AssetInfo.VERSION} " + (!AssetInfo.VersionChecking.UPDATE_AVAILABLE ? "(latest)" : string.Empty));
            float versionWidth = EditorStyles.miniLabel.CalcSize(version).x;

            Rect versionRect = new Rect(rect.x + totalWidth, rect.y, versionWidth, titleSize.y);
            totalWidth += versionRect.width;
            GUI.Label(versionRect, version, EditorStyles.miniLabel);

            if (AssetInfo.VersionChecking.UPDATE_AVAILABLE)
            {
                GUIContent update = new GUIContent($" Update to {AssetInfo.VersionChecking.LATEST_AVAILABLE}", EditorGUIUtility.IconContent("d_Package Manager").image);

                GUIStyle linkStyle = new GUIStyle(EditorStyles.centeredGreyMiniLabel);
                linkStyle.normal.textColor = Color.Lerp(LightBlueColor, LightBlueColor * 1.4f,
                    Mathf.Sin((float)EditorApplication.timeSinceStartup * 5f) * 0.5f + 0.5f);
                linkStyle.hover.textColor = LighterBlueColor;
    
                float updateWidth = linkStyle.CalcSize(update).x;
    
                Rect updateRect = new Rect(rect.x + (-updateWidth * 0.5f) + totalWidth * 0.5f, rect.y + titleSize.y + 1f, updateWidth, titleSize.y);
    
                //Make it clickable
                EditorGUIUtility.AddCursorRect(updateRect, MouseCursor.Link);
    
                if (GUI.Button(updateRect, update, linkStyle))
                {
                    AssetInfo.OpenInPackageManager();
                }
            }
        }
        
        public class Section
        {
            public bool Expanded
            {
                get => SessionState.GetBool(id, false);
                set => SessionState.SetBool(id, value);
            }
            public readonly AnimBool anim;

            private readonly string id;
            private readonly GUIContent content;
            public SectionEditor editor;

            public Section(Editor owner, string id, GUIContent title)
            {
                this.id = $"SM2_{id}_SECTION";
                this.content = title;

                anim = new AnimBool(true);
                anim.valueChanged.AddListener(owner.Repaint);
                anim.speed = 12f;
                anim.target = Expanded;
            }

            public static Section Create<T>(Editor owner, string id, GUIContent title, SerializedProperty settings) where T : SectionEditor, new()
            {
                Section section = new Section(owner, id, title);
                
                section.editor = SectionEditor.Create<T>(owner.target, owner.serializedObject, settings);

                return section;
            }
                
            public void DrawHeader(Action clickAction)
            {
                UI.DrawSectionHeader(content, Expanded, clickAction);
                anim.target = Expanded;
            }

            public void DrawUI(ref bool changed)
            {
                editor.OnInspectorGUI(ref changed);
            }

            public void DrawSceneGUI()
            {
                editor.OnSceneGUI();
            }

            public void Disable()
            {
                editor.OnDisable();
            }
            
            public class SectionEditor
            {
                protected Object target;
                protected SerializedObject serializedObject;
                protected SerializedProperty settings;

                public static T Create<T>(Object target, SerializedObject serializedObject, SerializedProperty property) where T : SectionEditor, new()
                {
                    T editor = new T();
                    editor.target = target;
                    editor.serializedObject = serializedObject;
                    editor.settings = property;

                    editor.OnEnable();
            
                    return editor;
                }
            
                public virtual void OnEnable()
                {
                
                }

                public virtual void OnInspectorGUI(ref bool changed)
                {

                }

                public virtual void OnSceneGUI()
                {
                    
                }

                public virtual void OnDisable()
                {
                    
                }
            }
        }
        
        public static void DrawH2(string text)
        {
            Rect backgroundRect = EditorGUILayout.GetControlRect();
            backgroundRect.height = 25f;
            
            var labelRect = backgroundRect;

            // Background rect should be full-width
            backgroundRect.xMin = 0f;

            // Background
            float backgroundTint = (EditorGUIUtility.isProSkin ? 0.1f : 1f);
            EditorGUI.DrawRect(backgroundRect, new Color(backgroundTint, backgroundTint, backgroundTint, 0.2f));

            // Title
            EditorGUI.LabelField(labelRect, new GUIContent(text), Styles.H2);
            
            EditorGUILayout.Space(backgroundRect.height * 0.5f);
        }

        public static void DrawSplitter(bool isBoxed = false)
        {
            var rect = GUILayoutUtility.GetRect(1f, 1f);
            float xMin = rect.xMin;

            // Splitter rect should be full-width
            rect.xMin = 0f;
            rect.width += 4f;
            
            if (isBoxed)
            {
                rect.xMin = xMin == 7.0f ? 4.0f : EditorGUIUtility.singleLineHeight;
                rect.width -= 1;
            }

            if (Event.current.type != EventType.Repaint)
                return;

            EditorGUI.DrawRect(rect, !EditorGUIUtility.isProSkin
                ? new Color(0.6f, 0.6f, 0.6f, 1.333f)
                : new Color(0.12f, 0.12f, 0.12f, 1.333f));
        }

        public static bool DrawSectionHeader(GUIContent content, bool isExpanded, Action clickAction)
        {
            DrawSplitter();
            
            var e = Event.current;
            Rect backgroundRect = GUILayoutUtility.GetRect(1f, HeaderHeight);
            
            var labelRect = backgroundRect;
            labelRect.xMin += 8f;
            labelRect.xMax -= 20f + 16 + 5;
            
            Texture icon = content.image;
            
            var iconRect = labelRect;
            iconRect.width = 16;
            iconRect.height = 16;
            iconRect.y += 4f;

            if (icon)
            {
                labelRect.x += iconRect.width + 6;
            }

            var foldoutRect = backgroundRect;
            foldoutRect.xMin -= 8f;
            foldoutRect.y += 0f;
            foldoutRect.width = HeaderHeight;
            foldoutRect.height = HeaderHeight;

            //Background rect should be full-width
            backgroundRect.xMin = 0f;
            backgroundRect.width += 4f;

            bool highlighted = backgroundRect.Contains(e.mousePosition);
            
            //Expand when dragging something over it, user wants access
            if (highlighted)
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Generic;

                if (e.type == EventType.DragUpdated)
                {
                    if (clickAction != null && !isExpanded) clickAction.Invoke();
                    isExpanded = true;
                    
                    DragAndDrop.AcceptDrag();
                }
            }
            
            //Background
            float backgroundTint = (EditorGUIUtility.isProSkin ? 0.1f : 1f);
            if (highlighted) backgroundTint *= EditorGUIUtility.isProSkin ? 1.5f : 0.9f;
            
            EditorGUI.DrawRect(backgroundRect, new Color(backgroundTint, backgroundTint, backgroundTint, 0.2f));

            //Icon
            if (icon)
            {
                GUI.DrawTexture(iconRect, icon);
            }
            
            EditorGUI.LabelField(labelRect, content.text, EditorStyles.boldLabel);

            //Foldout
            GUI.Label(foldoutRect, new GUIContent(isExpanded ? "−" : "≡"), EditorStyles.boldLabel);

            if (backgroundRect.Contains(e.mousePosition))
            {
                if (e.type == EventType.MouseDown)
                {
                    if (e.button == 0)
                    {
                        isExpanded = !isExpanded;
                        if(clickAction != null) clickAction.Invoke();
                    }

                    e.Use();
                }
                
            }
            
            return isExpanded;
        }
        
        public static void DrawToolInstructions(UnityEditor.SceneView sceneView)
        {
            Handles.BeginGUI();
            Rect pixelRect = EditorGUIUtility.PixelsToPoints(sceneView.camera.pixelRect);

            float screenHeight = pixelRect.height;
            float width = 160f;
            float height = 95f;
            Rect windowRect = new Rect(pixelRect.width * 0.5f - (width * 0.5f), screenHeight - height * 0.5f - 15, width, height);
            
            GUILayout.BeginArea(windowRect);
            {
                GUIStyle style = new GUIStyle(EditorStyles.miniLabel);
                style.richText = true;
                
                GUILayout.BeginVertical(EditorStyles.textArea);
                GUILayout.Space(5f);
                GUILayout.Label($"<b>Left-click</b>: Add point", style);
                GUILayout.Label($"<b>Left-click+Drag</b>: Move point", style);
                GUILayout.Label($"<b>Right-click</b>: Delete point", style);
                GUILayout.Space(5f);
                GUILayout.EndHorizontal();
            }
            GUILayout.EndArea();
            Handles.EndGUI();
        }

        public static void DrawToolToggle<T>(Texture2D icon) where T : EditorTool
        {
            var active = SplineMesherEditor.IsToolActive<T>();
            
            string label = active ? "Close" : "Open";
            
            EditorGUI.BeginChangeCheck();
            GUILayout.Toggle(active, new GUIContent($"  {label} Editor", icon), "Button", GUILayout.Width(120f), GUILayout.Height(EditorGUIUtility.singleLineHeight + 5f));
            
            if (EditorGUI.EndChangeCheck())
            {
                if (!active) SplineMesherEditor.OpenTool<T>();
                else SplineMesherEditor.CloseTool<T>();
            }
        }
        
        private static string[] renderingLayerNames;
        public static void DrawRenderingLayerMask(SerializedProperty property)
        {
            #if SRP && UNITY_2022_3_OR_NEWER
            #if UNITY_6000_0_OR_NEWER
            renderingLayerNames = RenderingLayerMask.GetDefinedRenderingLayerNames();
            #else
            var renderPipeline = GraphicsSettings.currentRenderPipeline;

            if (renderPipeline == null)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.PrefixLabel(property.displayName);
                    EditorGUILayout.LabelField("Only available when a SRP is active", EditorStyles.miniLabel,
                        GUILayout.MaxWidth(EditorGUIUtility.labelWidth + EditorGUIUtility.fieldWidth));
                }

                return;
            }
            renderingLayerNames = renderPipeline.prefixedRenderingLayerMaskNames;
            #endif
            
            GUIContent content = new GUIContent(property.displayName, property.tooltip);
            
            var rect = EditorGUILayout.GetControlRect();
            EditorGUI.showMixedValue = property.hasMultipleDifferentValues;
            EditorGUI.BeginProperty(rect, content, property);
            EditorGUI.BeginChangeCheck();
            
            var mask = (uint)property.uintValue;
            mask = (uint)EditorGUI.MaskField(rect, content, (int)mask, renderingLayerNames);
            
            if (EditorGUI.EndChangeCheck())
            {
                property.uintValue = mask;
            }
            EditorGUI.EndProperty();
            EditorGUI.showMixedValue = false;
            #else
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.PrefixLabel(property.displayName);
                EditorGUILayout.LabelField("Not available in the Built-in RP", EditorStyles.miniLabel, GUILayout.MaxWidth(EditorGUIUtility.labelWidth + EditorGUIUtility.fieldWidth));
            }
            #endif
        }

        public static void DrawLayerDropdown(SerializedProperty property)
        {
            int layer = property.intValue;
    
            GUIContent content = new GUIContent(property.displayName, property.tooltip);
    
            var rect = EditorGUILayout.GetControlRect();
            EditorGUI.showMixedValue = property.hasMultipleDifferentValues;
            EditorGUI.BeginProperty(rect, content, property);
            EditorGUI.BeginChangeCheck();

            rect.width = EditorGUIUtility.labelWidth + 150f;
            int newLayer = EditorGUI.LayerField(rect, content, layer);
    
            if (EditorGUI.EndChangeCheck())
            {
                property.intValue = newLayer;
            }
            EditorGUI.EndProperty();
            EditorGUI.showMixedValue = false;
        }

        public static void DrawAlignmentSelector(SerializedProperty property)
        {
            EditorGUILayout.Separator();
            
            Structs.Alignment alignment = (Structs.Alignment)property.intValue;
    
            GUIContent content = new GUIContent(property.displayName, property.tooltip);

            float scale = 4f;
            var rect = EditorGUILayout.GetControlRect(true, EditorGUIUtility.singleLineHeight * scale + 10);
            EditorGUI.BeginProperty(rect, content, property);
            EditorGUI.BeginChangeCheck();
            
            //Draw label
            var labelRect = new Rect(rect.x, rect.y, EditorGUIUtility.labelWidth, EditorGUIUtility.singleLineHeight);
            EditorGUI.LabelField(labelRect, content);
            labelRect.y += EditorGUIUtility.singleLineHeight;
            EditorGUI.LabelField(labelRect, $"({System.Text.RegularExpressions.Regex.Replace(alignment.ToString(), "(\\B[A-Z])", " $1")})", EditorStyles.miniLabel);
            
            //Create rectangle area for buttons
            float rectSize = EditorGUIUtility.singleLineHeight * scale;
            float rectX = rect.x + EditorGUIUtility.labelWidth + 5;
            float rectY = rect.y;
            float buttonSize = rectSize / 3f;
            
            //Draw background rectangle
            Rect backgroundRect = new Rect(rectX, rectY, rectSize, rectSize);
            EditorGUI.DrawRect(backgroundRect, new Color(0.2f, 0.2f, 0.2f, 0.3f));
            
            //Define button positions (3x3 grid)
            Structs.Alignment[,] alignmentGrid = new Structs.Alignment[3, 3]
            {
                { Structs.Alignment.TopLeft, Structs.Alignment.TopCenter, Structs.Alignment.TopRight },
                { Structs.Alignment.MiddleLeft, Structs.Alignment.MiddleCenter, Structs.Alignment.MiddleRight },
                { Structs.Alignment.BottomLeft, Structs.Alignment.BottomCenter, Structs.Alignment.BottomRight }
            };
            
            //Draw buttons
            for (int row = 0; row < 3; row++)
            {
                for (int col = 0; col < 3; col++)
                {
                    Rect buttonRect = new Rect(
                        rectX + col * buttonSize,
                        rectY + row * buttonSize,
                        buttonSize,
                        buttonSize
                    );
                    
                    Structs.Alignment buttonAlignment = alignmentGrid[row, col];
                    bool isSelected = alignment == buttonAlignment;
                    
                    
                    if (GUI.Button(buttonRect, new GUIContent("", buttonAlignment.ToString()), GUI.skin.button))
                    {
                        if(alignment == buttonAlignment) alignment = Structs.Alignment.PivotPoint;
                        else alignment = buttonAlignment;
                    }
                    
                    //Draw button with highlight for selected
                    if (isSelected)
                    {
                        EditorGUI.DrawRect(buttonRect, new Color(0.3f, 0.5f, 0.8f, 0.5f));
                    }
                    //Draw a small dot to indicate the position
                    Rect dotRect = new Rect(
                        buttonRect.x + buttonRect.width / 2f - 3,
                        buttonRect.y + buttonRect.height / 2f - 3,
                        6, 6
                    );
                    EditorGUI.DrawRect(dotRect, isSelected ? GUI.skin.toggle.onFocused.textColor : new Color(0.5f, 0.5f, 0.5f, 0.8f));
                }
            }

            if (EditorGUI.EndChangeCheck())
            {
                property.intValue = (int)alignment;
            }
            
            EditorGUI.EndProperty();
        }
        
        public static void DrawFooter()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("- Staggart Creations -", EditorStyles.centeredGreyMiniLabel);
        }
        
        public static class SceneView
        {
            public static void DrawLabel(Vector3 position, string text, float offset = 1f)
            {
                var labelOffset = (HandleUtility.GetHandleSize(position) / 1.5f) * offset;
            
                Handles.Label(position + new Vector3(0, -labelOffset, 0), text, Styles.SceneLabel);
            }
            
            public static void DrawBoxedLabel(Vector3 position, string text)
            {
                var labelOffset = HandleUtility.GetHandleSize(position) * 0.25f;
                position += new Vector3(0, labelOffset, 0);

                Vector2 screenPos = HandleUtility.WorldToGUIPoint(position);
                Rect r = new Rect(screenPos.x, screenPos.y, Styles.SceneLabel.CalcSize(new GUIContent(text)).x + 5f, 22f);
            
                //Center
                r.x -= r.width * 0.5f;
            
                GUI.color = EditorGUIUtility.isProSkin ? Color.white * 0.5f : Color.gray;
                if (EditorGUIUtility.isProSkin)
                {
                    GUI.Box(r, "", EditorStyles.textArea);
                }

                GUI.color = Color.white * 2f;
                GUI.Label(r, text, Styles.SceneLabel);
                //Handles.Label(position , "End");
            }
        }

        public static class Icons
        {
            public static string prefix => EditorGUIUtility.isProSkin ? "d_" : string.Empty;
            
            private static Texture2D LoadFromResources(string path)
            {
                string absolutePath = $"{SplineMesher.kPackageRoot}/Editor/Resources/{path}";
                Texture2D icon = AssetDatabase.LoadAssetAtPath<Texture2D>(absolutePath);

                if (!icon)
                {
                    Debug.LogError($"Failed to load icon {absolutePath}");
                    return EditorGUIUtility.IconContent("_Help").image as Texture2D;
                }

                return icon;
            }

            private static Texture2D LoadNative(string path) => EditorGUIUtility.IconContent(path).image as Texture2D;

            public static Texture2D LoadFromData(string data, int size = 32)
            {
                byte[] bytes = Convert.FromBase64String(data);

                Texture2D icon = new Texture2D(size, size, TextureFormat.RGBA32, false, false);
                icon.LoadImage(bytes, true);
        
                return icon;
            }
            
            private static Texture2D _Edition;
            public static Texture2D Edition => _Edition ??= LoadFromResources("Icons/spline-mesher-edition-icon.psd");
            
            private static Texture2D _Mesh;
            public static Texture2D Mesh => _Mesh ??= LoadNative("Mesh Icon");
            
            private static Texture2D _Renderer;
            public static Texture2D Renderer => _Renderer ??= LoadNative("Material Icon");
            
            private static Texture2D _Topology;
            public static Texture2D Topology => _Topology ??= LoadFromResources("Icons/spline-topology-icon-64px.psd");
            
            private static Texture2D _Displacement;
            public static Texture2D Displacement => _Displacement ??= LoadFromResources("Icons/spline-displacement-icon-64px.psd");
            
            private static Texture2D _Distribution;
            public static Texture2D Distribution => _Distribution ??= LoadFromResources("Icons/spline-distribution-icon-64px.psd");
            
            private static Texture2D _Scale;
            public static Texture2D Scale => _Scale ??= LoadFromResources("Tools/spline-mesher-scale-icon-64px.psd");
            
            private static Texture2D _Roll;
            public static Texture2D Roll => _Roll ??= LoadFromResources("Tools/spline-mesher-roll-icon-64px.psd");
            
            private static Texture2D _UV;
            public static Texture2D UV => _UV ??= LoadFromResources("Icons/spline-uv-icon-64px.psd");
            
            private static Texture2D _VertexColors;
            public static Texture2D VertexColors => _VertexColors ??= LoadFromResources("Tools/spline-mesher-color-icon-64px.psd");
            
            private static Texture2D _Conforming;
            public static Texture2D Conforming => _Conforming ??= LoadFromResources("Tools/spline-mesher-conforming-icon-64px.psd");
            
            private static Texture2D _Collision;
            public static Texture2D Collision => _Collision ??= LoadNative("BoxCollider Icon");
            
            private static Texture2D _GameObject;
            public static Texture2D GameObject => _GameObject ??= LoadNative("GameObject Icon");
            
            private static Texture2D _Cap;
            public static Texture2D Cap => _Cap ??= LoadFromResources("Icons/spline-cap-icon-64px.psd");
            
            private static Texture2D _Event;
            public static Texture2D Event => _Event ??= LoadNative("EventSystem Icon");
            
        }
        
        public static class Styles
        {
            private static GUIStyle _Section;
            public static GUIStyle Section
            {
                get
                {
                    if (_Section == null)
                    {
                        _Section = new GUIStyle()
                        {
                            margin = new RectOffset(0, 0, -5, 5),
                            padding = new RectOffset(10, 10, 5, 5),
                            clipping = TextClipping.Clip,
                        };
                    }

                    return _Section;
                }
            }
            
            private static GUIStyle _H2;
            public static GUIStyle H2
            {
                get
                {
                    if (_H2 == null)
                    {
                        _H2 = new GUIStyle(GUI.skin.label)
                        {
                            richText = true,
                            alignment = TextAnchor.MiddleLeft,
                            wordWrap = true,
                            fontSize = 14,
                            fontStyle = FontStyle.Bold,
                            padding = new RectOffset(10, 0, 0, 0)
                        };
                    }

                    return _H2;
                }
            }
            
            private static GUIStyle _Button;
            public static GUIStyle Button
            {
                get
                {
                    if (_Button == null)
                    {
                        _Button = new GUIStyle(GUI.skin.button)
                        {
                            alignment = TextAnchor.MiddleLeft,
                            stretchWidth = true,
                            richText = true,
                            wordWrap = true,
                            padding = new RectOffset()
                            {
                                left = 14,
                                right = 14,
                                top = 8,
                                bottom = 8
                            }
                        };
                    }

                    return _Button;
                }
            }

            private static GUIStyle _Header;
            public static GUIStyle Header
            {
                get
                {
                    if (_Header == null)
                    {
                        _Header = new GUIStyle(GUI.skin.label)
                        {
                            richText = true,
                            alignment = TextAnchor.MiddleCenter,
                            wordWrap = true,
                            fontSize = 18,
                            fontStyle = FontStyle.Normal
                        };
                    }

                    return _Header;
                }
            }
            
            private static GUIStyle _Footer;
            public static GUIStyle Footer
            {
                get
                {
                    if (_Footer == null)
                    {
                        _Footer = new GUIStyle(EditorStyles.centeredGreyMiniLabel)
                        {
                            richText = true,
                            alignment = TextAnchor.MiddleCenter,
                            wordWrap = true,
                            fontSize = 12
                        };
                    }

                    return _Footer;
                }
            }
            
            private static GUIStyle _Label;
            public static GUIStyle SceneLabel
            {
                get
                {
                    if (_Label == null)
                    {
                        _Label = new GUIStyle(EditorStyles.largeLabel)
                        {
                            alignment = TextAnchor.MiddleLeft,
                            fontSize = 12,
                            fontStyle = FontStyle.Bold,
                            padding = new RectOffset()
                            {
                                left = 5,
                                right = 0,
                                top = 0,
                                bottom = 0
                            }
                        };
                    
                        _Label.normal.textColor = Color.black; // Set the text color to black
                        _Label.normal.background = Texture2D.whiteTexture;
                    }

                    return _Label;
                }
            }
        }
    }
}