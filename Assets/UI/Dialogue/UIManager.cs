using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class UIManager : MonoBehaviour
{
   private static UIManager _instance;

   private void Awake()
   {
      if (_instance != null)
      {
         Destroy(gameObject); //单例模式，如果UIManager已经实例化，那么就删除当前对象（不需要再实例化UIManager),返回，否则就实例化
         return;
      }

      _instance = this;
      DontDestroyOnLoad(gameObject);//跨场景不销毁
   }

   [SerializeField]private DialogueBox _dialogueBox;
   //打开对话框
   public static void OpenDialogueBox()
   {
      _instance._dialogueBox.Open();
   }

   public static void CloseDialogueBox()
   {
      _instance._dialogueBox.Close();
   }

   public static void PrintDialogue(DialogueData data)
   {
      
   }
}
