namespace TahaKonsept.Agent.Engines.Video.Engines;

/// <summary>Render için hazırlanmış sahne projesi (motorun kendi dosya düzeninde).</summary>
/// <param name="ProjectDir">Proje klasörü.</param>
/// <param name="AssetsDir">Sahne görsellerinin kopyalandığı klasör.</param>
/// <param name="EntryPath">Giriş dosyası (index.html, index.ts, render.mjs…).</param>
/// <param name="DeleteAfterRender">Render sonrası klasör silinsin mi (şablon içi geçici projeler).</param>
public sealed record SceneProject(string ProjectDir, string AssetsDir, string EntryPath, bool DeleteAfterRender);
