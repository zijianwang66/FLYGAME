using UnityEditor;
using UnityEngine;

namespace DroneMicroClass.Editor
{
    public sealed class ActivationCodeGeneratorWindow : EditorWindow
    {
        private string machineCode = string.Empty;
        private string activationCode = string.Empty;

        [MenuItem("Drone MicroClass/Activation/Generate Activation Code")]
        public static void Open()
        {
            ActivationCodeGeneratorWindow window = GetWindow<ActivationCodeGeneratorWindow>("Activation Code");
            window.minSize = new Vector2(520f, 180f);
            window.machineCode = ActivationManager.MachineCode;
            window.Generate();
            window.Show();
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("客户机器码", EditorStyles.boldLabel);
            machineCode = EditorGUILayout.TextField(machineCode);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("生成激活码", GUILayout.Height(32f)))
                {
                    Generate();
                }

                if (GUILayout.Button("复制激活码", GUILayout.Height(32f)))
                {
                    GUIUtility.systemCopyBuffer = activationCode;
                }
            }

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("激活码", EditorStyles.boldLabel);
            EditorGUILayout.SelectableLabel(activationCode, EditorStyles.textField, GUILayout.Height(24f));

            EditorGUILayout.HelpBox("把客户软件启动页显示的机器码填到上面，生成后把激活码发给客户。激活码只对对应电脑有效。", MessageType.Info);
        }

        private void Generate()
        {
            activationCode = string.IsNullOrWhiteSpace(machineCode)
                ? string.Empty
                : ActivationManager.GenerateActivationCode(machineCode);
        }
    }
}
