using OkulYonetim.Tests.Integration.Infrastructure;

// Tüm entegrasyon testleri koşu başına tek bir test veritabanını paylaşır (aç → kullan → sil).
[assembly: AssemblyFixture(typeof(TestDatabase))]
