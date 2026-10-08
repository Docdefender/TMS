# Sürüm doğrulama ve geri dönüş

Bu belge yerel TMS kurulumunun sürüm kapısıdır. Canlı ortama yayın yapılmış veya tam geri yükleme kanıtlanmış sayılmaz.

## Yayın öncesi

1. `dotnet build TMS.csproj --no-restore -c Release` ve `dotnet run --project Tests/AccessChecks/AccessChecks.csproj --no-launch-profile -c Release -p:UseAppHost=false -- --sql` başarılı olmalı. Kritik Admin, Manager, Member erişimi; pipeline; Ticket; bildirim; SLA; otomatik atama; rapor ve zaman takibi test kapsamındadır. Son kabul sonucu 16 yetki, 117 SQL ve 91 HTTP senaryosudur.
2. Hedef SQL Server sürümü yedeğin alındığı sürümle aynı veya daha yeni olmalı. Veritabanı migration geçmişi ve bekleyen migration'lar kontrol edilmeli. Şema yükseltmesi için önce veritabanı ve `wwwroot/uploads` birlikte yedeklenmeli.
3. Yedek dosyasının SHA-256 özeti manifest ile eşleşmeli; ek dosyaları sayılıp okunabilirliği kontrol edilmeli. `BACKUP ... WITH CHECKSUM` ve `msdb` kontrolü tek başına geri yükleme kanıtı değildir.
4. Aynı/yeni sürüm SQL Server üzerinde yetkili hesapla, **Tms dışında yeni ve boş bir test veritabanına** `RESTORE VERIFYONLY` ve tam `RESTORE DATABASE ... WITH MOVE` uygulanmalı. Uygulama verisi, `__EFMigrationsHistory`, proje/görev sayıları ve eklerin açılması test edilmeli. Test veritabanı iş bitince kaldırılabilir; üretim veritabanı üzerine geri yükleme yapılmamalı.
5. Yayımlama paketinde gerçek bağlantı parolası veya kullanıcı yüklemesi bulunmamalı. Geliştirme makinesindeki bağlantı `appsettings.Local.json` içindedir; dosya Git dışında tutulur ve yayıma kopyalanmaz. `wwwroot/uploads` da pakete girmez; dağıtımda ayrı kalıcı depodan sağlanır. Hedef ortam `ConnectionStrings__DefaultConnection` değişkenini gizli yapılandırmasından sağlamalı. İlk kurulumda yönetici yoksa `TMS_BOOTSTRAP_ADMIN_PASSWORD` ortam değişkeni güçlü bir değerle sağlanmalı; mevcut yönetici varsa kullanılmaz. Daha önce izlenen dosyada bulunan bağlantı parolası Git geçmişinde kalmış olabileceğinden yayın öncesi döndürülmeli.

## Yayın ve geri dönüş

1. Uygulamayı durdur; eşzamanlı yazıları kes. Veritabanı ve yüklenen dosyalar için aynı anı temsil eden yeni yedek al; manifest ve özeti ayrı güvenli yerde sakla.
2. Hazırlanmış sürüm dosyalarını ve hedef ortam gizli yapılandırmasını yükle. Migration'ları inceleyerek uygula. Uygulamayı başlatıp giriş, Admin/Manager/Member yetkileri, proje ve pipeline ekranı, görev kaydı ve ek erişimi için kısa kabul turu yap.
3. Kritik hata varsa uygulamayı durdur. Önceki uygulama dosyalarını, **aynı yedek çiftinden** veritabanını ve yüklenen dosyaları geri getir. Uygulamayı başlatıp aynı kabul turunu tekrarla. Migration geri alma komutuna tek başına güvenme; veri dönüşümü ve silme geri alınamayabilir.

## 2026-10-08 yerel durum

- Release derlemesi başarılı: 0 uyarı, 0 hata.
- İzole test veritabanında 16 yetki, 117 SQL ve 91 HTTP senaryosu geçti (224 toplam).
- Temiz Release yayın paketi oluşturuldu. `appsettings.Local.json` ve `wwwroot/uploads` pakete girmiyor; paketlenen `appsettings.json` bağlantısı boş.
- Paket sürümleri ve yerel EF aracı .NET 8 hattında sabitlendi. GitHub Actions, Windows ve LocalDB üzerinde aynı derleme ile testleri çalıştıracak şekilde hazırlandı.
- İlk bilgisayar kurulumu için `scripts/Setup-Local.ps1`, örnek yerel ayar ve kök README hazırlandı. Temiz ikinci bilgisayar kabulü henüz yapılmadı.
- Eski Git geçmişindeki iki `appsettings.json` sürümünde parola içeren SQL bağlantısı tespit edildi. Açık kaynak yayından önce ilgili parola döndürülmeli ve repository geçmişi temiz başlangıç sürümüne dönüştürülmelidir.
