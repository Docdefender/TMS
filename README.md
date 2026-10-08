# Dizge

Dizge; departman, proje, görev, pipeline ve kurum içi destek taleplerini tek çalışma alanında birleştiren ASP.NET Core tabanlı bir iş yönetimi uygulamasıdır.

## Öne çıkan özellikler

- Admin, Manager ve Member için sunucu tarafında doğrulanan rol ve departman yetkileri
- Proje listesi, Kanban, görev Board'u ve proje bazlı pipeline
- Aşama, checkpoint, görev ve proje bağımlılıkları ile zaman çizelgesi
- Ticket havuzu, özel görüntüleyiciler, iç notlar, dosyalar ve göreve dönüştürme
- Ticket önceliği, SLA çözüm hedefi ve isteğe bağlı iş yükü bazlı otomatik atama
- Uygulama içi bildirim merkezi
- Görev sayacı, elle süre girişi ve kişi bazlı zaman kayıtları
- Rolün erişim alanına göre proje, görev, Ticket ve iş yükü raporları
- Açık/koyu tema, masaüstü ve mobil uyumlu Türkçe arayüz

Microsoft 365 posta alma kodu kaynakta korunur ancak varsayılan olarak kapalıdır. Uygulama e-posta okumaz veya göndermez.

## Teknoloji

- .NET 8 / ASP.NET Core Razor Pages
- Entity Framework Core 8
- ASP.NET Core Identity
- SQL Server veya SQL Server Express
- Yerel CSS ve JavaScript; çalışma zamanında harici CSS çerçevesi kullanılmaz

## Windows üzerinde ilk kurulum

Gerekenler:

1. [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
2. SQL Server 2019+ veya SQL Server Express
3. Git

Depoyu klonladıktan sonra:

```powershell
git clone https://github.com/Docdefender/TMS.git
cd TMS\TMS
powershell -ExecutionPolicy Bypass -File .\scripts\Setup-Local.ps1
```

İlk çalıştırmada `appsettings.Local.json` oluşturulur. Varsayılan bağlantı `localhost\SQLEXPRESS` üzerindeki `Dizge` veritabanını ve Windows kimlik doğrulamasını kullanır. Sunucu adınız farklıysa bu dosyayı düzenleyip betiği tekrar çalıştırın.

Veritabanı hazırlandıktan sonra betik ilk Admin parolasını ister. İlk hesap:

- E-posta: `admin@tms.com`
- Parola: kurulum sırasında verdiğiniz parola

Parola kaynak kodda veya ayar dosyasında saklanmaz. Sonraki çalıştırmalarda parola sorusunu boş bırakabilirsiniz.

## Elle kurulum

`appsettings.Local.example.json` dosyasını `appsettings.Local.json` adıyla kopyalayın ve bağlantıyı düzenleyin. Ardından:

```powershell
dotnet tool restore
dotnet restore
dotnet ef database update
$env:TMS_BOOTSTRAP_ADMIN_PASSWORD = "geçici-ilk-parolanız"
dotnet run --launch-profile http
```

İlk başarılı açılıştan sonra ortam değişkenini kaldırın:

```powershell
Remove-Item Env:TMS_BOOTSTRAP_ADMIN_PASSWORD
```

Uygulama `http://localhost:5088` adresinde açılır. Demo rol girişleri yalnızca Development ortamında görünür.
İlk yerel açılışta Manager ve Member demo hesapları otomatik hazırlanır. Giriş ekranındaki **Admin**, **Manager** ve **Member** düğmeleri yalnızca aynı bilgisayardan erişimde çalışır; üretim ortamında gösterilmez.

## Yapılandırma ve özel veriler

Aşağıdakiler Git tarafından özellikle dışlanır:

- `appsettings.Local.json`
- `.local-backups/`, `.local-build/`, `.local-runtime/`
- `wwwroot/uploads/`
- derleme ve IDE çıktıları

Üretimde bağlantı bilgisini ortam değişkeni veya güvenli bir yapılandırma sağlayıcısı ile verin:

```text
ConnectionStrings__DefaultConnection
```

## Doğrulama

```powershell
dotnet build TMS.csproj --no-restore -c Release
dotnet run --project Tests/AccessChecks/AccessChecks.csproj --no-launch-profile -c Release -p:UseAppHost=false -- --sql
```

Test paketi geçici ve izole bir LocalDB veritabanı oluşturur; yapılandırılmış uygulama veritabanını kullanmaz.

## Belgeler

- [Roadmap](TMS/docs/roadmap.md)
- [Ürün ve yetki kuralları](TMS/docs/product-rules.md)
- [Tasarım sistemi](TMS/docs/design-system.md)
- [Sürüm kontrol listesi](TMS/docs/release-checklist.md)
- [Microsoft 365 altyapısının kapalı durumu](TMS/docs/microsoft365-mail-integration.md)

## Lisans

Bu proje [MIT Lisansı](LICENSE) ile yayımlanır.
