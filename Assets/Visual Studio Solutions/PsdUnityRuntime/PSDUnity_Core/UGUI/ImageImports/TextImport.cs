using TMPro;
using UnityEngine;

namespace PSDUnity.UGUI
{
    public class TextImport : ImageImport
    {
        [Header("[可选-----------------------------------")]
        [SerializeField] protected float textBorder = 0.6f;//生成Text时,需要一定的边距
        [SerializeField] protected TextAlignmentOptions textAnchor = TextAlignmentOptions.TopLeft;
        [SerializeField] protected bool enableWordWrapping = true;
        [SerializeField] protected TextOverflowModes overflowMode = TextOverflowModes.Overflow;
        public override GameObject CreateTemplate()
        {
            var text = new GameObject("Text", typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
            text.alignment = textAnchor;
            text.enableWordWrapping = true;
            text.overflowMode = TextOverflowModes.Overflow;
            return text.gameObject;
        }

        public override UGUINode DrawImage(Data.ImgNode image, UGUINode parent)
        {
            UGUINode node = CreateRootNode(image.Name, AdjustTextRect( image.rect,image.fontSize), parent);
            var myText = node.InitComponent<TextMeshProUGUI>();
            PSDImporterUtility.SetPictureOrLoadColor(image, myText);
            return node;
        }
     
        /// <summary>
        /// 调节字边距
        /// </summary>
        /// <param name="image"></param>
        /// <param name="fontSize"></param>
        /// <returns></returns>
        private Rect AdjustTextRect(Rect oldRect, float fontSize)
        {
            var rect = oldRect;
            rect.width += fontSize * textBorder;
            rect.height += fontSize * textBorder;
            return rect;
        }
    }
}
