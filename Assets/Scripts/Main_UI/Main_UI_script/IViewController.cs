using UnityEngine.UIElements;
using System;

/// <summary>
/// 全ての画面コントローラーが守るべきルールブック（インターフェース）
/// これを継承したクラスは、必ず Initialize と Dispose を持たなければならない
/// </summary>
public interface IViewController
{
    void Dispose();  // これだけでOK
}