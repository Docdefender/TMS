# Microsoft 365 Ticket posta bağlantısı

> **Durum — 8 Ekim 2026:** Postadan Ticket alma özelliği ürün kararıyla kapatıldı. Kod, veri modeli ve test yardımcıları ileride yeniden kullanılmak üzere korunuyor. Varsayılan ayarda arka plan posta işlemi başlatılmaz; eşleşmeyen gönderen ekranı ve gelen posta adresi kullanıcı arayüzünde gösterilmez.

## Seçilen yaklaşım

Uygulama Microsoft Graph üzerinden, oturum açmış bir kullanıcıya bağlı olmadan çalışan bir servis kimliğiyle posta kutusunun Gelen Kutusu'nu izler. Okunacak posta kutusu ayardan seçilir. Bu nedenle ilk denemede aynı Microsoft 365 kuruluşundaki kişisel bir posta kutusu, daha sonra BT'nin hazırlayacağı paylaşılan posta kutusu kullanılabilir.

Kişisel kutuyla test yapılırken kutudaki normal yazışmaların Ticket'a dönüşmemesi için `RequiredSubjectPrefix` alanı `[DIZGE TEST]` yapılır. Değişiklik takibinde önce yalnızca ileti kimliği, konu, gönderen ve tarih bilgisi alınır; ileti gövdesi ancak konu filtreden geçerse istenir. Böylece kişisel kutudaki diğer iletilerin içerikleri uygulamaya indirilmez. Yalnızca konusu bu ifadeyle başlayan iletiler işlenir ve bu ifade Ticket başlığından çıkarılır. Ayrılmış üretim kutusunda filtre boş bırakılabilir.

Kurumun destek adresi bir dağıtım grubuysa Graph bağlantısında doğrudan `MailboxAddress` olarak kullanılamaz. Grup adresi değişmeden kalabilir; iletilerin uygulamanın okuyacağı gerçek bir paylaşılan veya kullanıcı posta kutusuna teslim edilmesi gerekir.

## İlk dilimde çalışan davranış

- İlk bağlantıda yalnızca son 7 gün incelenir; süre ayardan değiştirilebilir.
- Sonraki kontroller Microsoft Graph delta işaretiyle yalnızca değişiklikleri alır.
- Kayıtlı kullanıcıdan gelen yeni konuşma bir Ticket açar.
- Aynı konuşmadaki yeni ileti mevcut Ticket'a yanıt olarak eklenir.
- Çözülmüş veya kapatılmış Ticket otomatik açılmaz; “yeni yanıt” inceleme işareti alır.
- Sistemde e-posta adresi bulunmayan gönderici eşleşmeyenler havuzuna girer.
- İnternet ileti kimliği tekildir; aynı e-posta tekrar işlendiğinde ikinci kayıt oluşmaz.
- Posta kutusu değiştirildiğinde her kutunun senkronizasyon işareti ayrı tutulur.

Bağlantı varsayılan olarak kapalıdır. Kimlik bilgileri girilmeden ağ çağrısı yapılmaz.

İlk denemede kişisel bir test kutusu, konu filtresi `[DIZGE TEST]` ve ilk tarama aralığı 1 gün olarak yalnızca yerel ayara kaydedildi. Bağlantı, Microsoft Entra bilgileri gelene kadar kapalı tutuluyor.

## Test için gerekli BT ayarları

1. Microsoft Entra ID'de uygulama kaydı oluşturulur.
2. Uygulamanın kimlik (tenant) ve uygulama (client) numaraları alınır.
3. Test için süreli bir istemci parolası oluşturulur. Canlı kullanımda sertifika tercih edilecektir.
4. Gelen ileti gövdesini okuyabilmesi için uygulamaya `Mail.Read` uygulama yetkisi verilir.
5. Exchange Online uygulama RBAC kapsamı yalnızca seçilen test posta kutusuyla sınırlandırılır. Kuruluş çapında sınırsız posta yetkisi bırakılmaz.
6. Seçilen posta kutusunun tam adresi bildirilir.

Yanıtların uygulamadan e-posta olarak gönderileceği ikinci dilimde `Mail.Send` yetkisi de aynı posta kutusu kapsamıyla eklenecektir.

## Yerel ayarlar

Gizli değerler dosyaya yazılmaz. Aşağıdaki ortam ayarları kullanılır:

```text
Ticketing__Microsoft365__Enabled=true
Ticketing__Microsoft365__MailboxAddress=okunabilir-posta-kutusu@example.com
Ticketing__Microsoft365__RequiredSubjectPrefix=[DIZGE TEST]
Ticketing__Microsoft365__TenantId=...
Ticketing__Microsoft365__ClientId=...
Ticketing__Microsoft365__ClientSecret=...
```

İlk doğrulamada `Enabled=false` tutulabilir; uygulama ve veritabanı posta bağlantısı olmadan normal çalışmaya devam eder.

Geçici Client Secret dosyaya kaydedilmeden test başlatmak için `scripts/Start-TicketMailTest.ps1` kullanılır. Script Tenant ID ve Client ID değerlerini parametre olarak alır, Client Secret'ı gizli giriş olarak ister ve yalnızca çalışan uygulama sürecine aktarır.

## Sonraki dilim

Ticket ekranındaki dış yanıt Microsoft Graph'ın iletiye yanıt verme işlemiyle gönderilecek. Gönderim sıraya alınacak; başarılı, bekleyen ve hatalı durumları kaydedilecek. İç notlar hiçbir zaman e-postaya çıkmayacak. Gönderim hatası Ticket yanıtını kaybettirmeyecek ve yetkili kullanıcıya yeniden deneme seçeneği verecek.
