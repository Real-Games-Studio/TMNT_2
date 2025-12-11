using System;
using UnityEngine;
using UnityEngine.UI;
using ZXing;
using ZXing.QrCode;
public class QrCodeCreator : MonoBehaviour
{
    public IBarcodeWriter barcodeWriter = new BarcodeWriter()
    {
        Format = BarcodeFormat.QR_CODE,
        Options = new QrCodeEncodingOptions
        {
            Height = 256,
            Width = 256,
            Margin = 1
        }
    };

    public RawImage rawImage;

    private Texture2D _texture;

    private void Start()
    {
        _texture = new Texture2D(256, 256);
    }

    public void CreateAndShowBarcode(string content)
    {
        CreateBarcode(content);
        ShowBarcode();
    }

    private void CreateBarcode(string content)
    {
        var writed = barcodeWriter.Write(content);
        _texture.SetPixels32(writed);
        _texture.Apply();
    }
    private void ShowBarcode()
    {
        rawImage.texture = _texture;
    }
}