// Sunucunun ürettiği dosyayı tarayıcıya indirtir (spec 0017, plan Risks 7): Blob → geçici nesne adresi → görünmez a[download] tıklaması →
// adres bırakılır. İstek ortak istemcide (api-client) yapılır; burada ağ çağrısı yok. Dosya sunucuda ya da tarayıcı deposunda saklanmaz.
export function saveFile(blob: Blob, fileName: string): void {
  const url = URL.createObjectURL(blob);
  const link = document.createElement("a");
  link.href = url;
  link.download = fileName;
  link.hidden = true;
  document.body.appendChild(link);
  link.click();
  link.remove();
  URL.revokeObjectURL(url);
}
