# İngilizce Öğrenme Sistemi --- Veritabanı Tasarımı

Bu doküman; kullanıcı hesabı, puan/seviye, global rank, kurslar,
üniteler ve kullanıcı ilerlemesini birbirinden ayıran önerilen
veritabanı yapısını gösterir.

> Not: Bu tasarım **EF Core kullanılmadan**, doğrudan SQL/ADO.NET/Npgsql
> benzeri erişim yöntemleriyle kullanılabilecek şekilde düşünülmüştür.

------------------------------------------------------------------------

## 1. Genel Mimari

``` text
                         ┌──────────────────┐
                         │      Users       │
                         ├──────────────────┤
                         │ Id (PK)          │
                         │ Username         │
                         │ Email            │
                         │ PasswordHash     │
                         │ RoleId (FK)      │
                         │ CreatedAt        │
                         └────────┬─────────┘
                                  │
                    ┌─────────────┴─────────────┐
                    │                           │
                    ▼                           ▼
          ┌──────────────────┐        ┌──────────────────────┐
          │    UserStats     │        │ UserCourseProgress   │
          ├──────────────────┤        ├──────────────────────┤
          │ UserId (PK/FK)   │        │ UserId (PK/FK)       │
          │ XP               │        │ CourseId (PK/FK)     │
          │ Level            │        │ Progress             │
          │ RankId (FK)      │        │ StartedAt            │
          │ UpdatedAt        │        │ CompletedAt          │
          └────────┬─────────┘        └──────────┬───────────┘
                   │                             │
                   ▼                             ▼
          ┌──────────────────┐          ┌──────────────────┐
          │      Ranks       │          │     Courses      │
          ├──────────────────┤          ├──────────────────┤
          │ Id (PK)          │          │ Id (PK)          │
          │ Name             │          │ Name             │
          │ MinXP            │          │ Description      │
          │ MaxXP            │          │ LanguageLevelId  │
          └──────────────────┘          │ OrderNo          │
                                        └────────┬─────────┘
                                                 │
                                                 ▼
                                        ┌──────────────────┐
                                        │      Units       │
                                        ├──────────────────┤
                                        │ Id (PK)          │
                                        │ CourseId (FK)    │
                                        │ Name             │
                                        │ Description      │
                                        │ OrderNo          │
                                        └────────┬─────────┘
                                                 │
                                                 ▼
                                        ┌──────────────────────┐
                                        │  UserUnitProgress    │
                                        ├──────────────────────┤
                                        │ UserId (PK/FK)       │
                                        │ UnitId (PK/FK)       │
                                        │ Progress             │
                                        │ Completed            │
                                        │ StartedAt            │
                                        │ CompletedAt          │
                                        └──────────────────────┘

          ┌──────────────────────┐
          │   LanguageLevels     │
          ├──────────────────────┤
          │ Id (PK)              │
          │ Code                 │
          │ Name                 │
          │ MinXP (opsiyonel)    │
          │ MaxXP (opsiyonel)    │
          └──────────┬───────────┘
                     │
                     ▼
                  Courses
```

------------------------------------------------------------------------

# 2. Users

Kullanıcının kimlik ve hesap bilgilerini tutar.

  -----------------------------------------------------------------------
  Kolon             Tip               Anahtar           Açıklama
  ----------------- ----------------- ----------------- -----------------
  Id                INTEGER/BIGINT    PK                Kullanıcının
                                                        benzersiz ID'si

  Username          VARCHAR           UNIQUE            Kullanıcı adı

  Email             VARCHAR           UNIQUE            E-posta adresi

  PasswordHash      VARCHAR/TEXT      \-                PasswordHasher
                                                        tarafından
                                                        oluşturulan hash

  RoleId            INTEGER           FK                Kullanıcının rolü

  CreatedAt         TIMESTAMP         \-                Kayıt tarihi
  -----------------------------------------------------------------------

### Örnek

    Id Username   Email                 PasswordHash     RoleId
  ---- ---------- --------------------- -------------- --------
     1 emirhan    emirhan@example.com   `AQAAAA...`           1

> `PasswordHash` kesinlikle React'e gönderilmemelidir.

------------------------------------------------------------------------

# 3. UserStats

Kullanıcının oyunlaştırma ve puan bilgilerini tutar.

  Kolon       Tip              Anahtar   Açıklama
  ----------- ---------------- --------- ---------------------------
  UserId      INTEGER/BIGINT   PK, FK    Users.Id
  XP          INTEGER          \-        Kullanıcının toplam XP'si
  Level       INTEGER          \-        Oyun/hesap seviyesi
  RankId      INTEGER          FK        Ranks.Id
  UpdatedAt   TIMESTAMP        \-        Son güncelleme

### Örnek

    UserId     XP   Level   RankId
  -------- ------ ------- --------
         1   3200       8        3

------------------------------------------------------------------------

# 4. Ranks

Global rank sistemini tanımlar.

  Kolon   Tip       Anahtar   Açıklama
  ------- --------- --------- -----------------------
  Id      INTEGER   PK        Rank ID
  Name    VARCHAR   UNIQUE    Rank adı
  MinXP   INTEGER   \-        Rank için minimum XP
  MaxXP   INTEGER   \-        Rank için maksimum XP

### Örnek

    Id Name         MinXP   MaxXP
  ---- ---------- ------- -------
     1 Bronze           0     999
     2 Silver        1000    2499
     3 Gold          2500    4999
     4 Platinum      5000    9999
     5 Diamond      10000   19999

------------------------------------------------------------------------

# 5. LanguageLevels

İngilizce seviyelerini tanımlar.

  Kolon   Tip       Anahtar   Açıklama
  ------- --------- --------- -------------------
  Id      INTEGER   PK        Seviye ID
  Code    VARCHAR   UNIQUE    A1, A2, B1...
  Name    VARCHAR   \-        Seviye açıklaması
  MinXP   INTEGER   \-        Opsiyonel
  MaxXP   INTEGER   \-        Opsiyonel

### Örnek

    Id Code   Name
  ---- ------ --------------------
     1 A1     Beginner
     2 A2     Elementary
     3 B1     Intermediate
     4 B2     Upper-Intermediate
     5 C1     Advanced
     6 C2     Proficient

> Dil seviyesi ile oyun seviyesi aynı şey değildir. İleride gerçek
> seviye değerlendirmesi eklenecekse `MinXP/MaxXP` yerine assessment
> sonuçları kullanılabilir.

------------------------------------------------------------------------

# 6. Courses

Öğrenme kurslarını tutar.

  Kolon             Tip       Anahtar   Açıklama
  ----------------- --------- --------- -------------------
  Id                INTEGER   PK        Kurs ID
  Name              VARCHAR   \-        Kurs adı
  Description       TEXT      \-        Kurs açıklaması
  LanguageLevelId   INTEGER   FK        LanguageLevels.Id
  OrderNo           INTEGER   \-        Kurs sırası
  IsActive          BOOLEAN   \-        Aktif/pasif

### Örnek

    Id Name           LanguageLevelId   OrderNo IsActive
  ---- ------------ ----------------- --------- ----------
     1 English A1                   1         1 true
     2 English A2                   2         2 true
     3 English B1                   3         3 false

------------------------------------------------------------------------

# 7. Units

Her kursun içindeki üniteleri tutar.

  Kolon         Tip       Anahtar   Açıklama
  ------------- --------- --------- ------------------
  Id            INTEGER   PK        Ünite ID
  CourseId      INTEGER   FK        Courses.Id
  Name          VARCHAR   \-        Ünite adı
  Description   TEXT      \-        Ünite açıklaması
  OrderNo       INTEGER   \-        Ünite sırası

### Örnek

    Id   CourseId Name           OrderNo
  ---- ---------- ------------ ---------
     1          1 Greetings            1
     2          1 Family               2
     3          1 Daily Life           3
     4          1 Food                 4

------------------------------------------------------------------------

# 8. UserCourseProgress

Kullanıcının kurs bazındaki ilerlemesini tutar.

  Kolon         Tip               Anahtar   Açıklama
  ------------- ----------------- --------- -----------------------
  UserId        INTEGER           PK, FK    Users.Id
  CourseId      INTEGER           PK, FK    Courses.Id
  Progress      INTEGER/DECIMAL   \-        Kurs ilerleme yüzdesi
  StartedAt     TIMESTAMP         \-        Başlangıç tarihi
  CompletedAt   TIMESTAMP         \-        Tamamlanma tarihi

### Composite Primary Key

``` text
(UserId, CourseId)
```

Aynı kullanıcı aynı kurs için iki ayrı kayıt oluşturamaz.

### Örnek

    UserId   CourseId   Progress Completed
  -------- ---------- ---------- -----------
         1          1         72 Hayır
         1          2         15 Hayır
         2          1        100 Evet

------------------------------------------------------------------------

# 9. UserUnitProgress

Kullanıcının ünite bazındaki ilerlemesini tutar.

  Kolon         Tip               Anahtar   Açıklama
  ------------- ----------------- --------- ------------------------
  UserId        INTEGER           PK, FK    Users.Id
  UnitId        INTEGER           PK, FK    Units.Id
  Progress      INTEGER/DECIMAL   \-        Ünite ilerleme yüzdesi
  Completed     BOOLEAN           \-        Tamamlandı mı?
  StartedAt     TIMESTAMP         \-        Başlangıç tarihi
  CompletedAt   TIMESTAMP         \-        Tamamlanma tarihi

### Composite Primary Key

``` text
(UserId, UnitId)
```

### Örnek

    UserId   UnitId   Progress Completed
  -------- -------- ---------- -----------
         1        1        100 true
         1        2         80 false
         1        3         25 false

------------------------------------------------------------------------

# 10. Relationships

## Users → UserStats

``` text
Users.Id
    │
    └────── UserStats.UserId
```

İlişki:

``` text
1 : 1
```

Her kullanıcının bir UserStats kaydı olması beklenir.

------------------------------------------------------------------------

## Users → UserCourseProgress

``` text
Users.Id
    │
    └────── UserCourseProgress.UserId
```

İlişki:

``` text
1 : N
```

Bir kullanıcı birçok kursun ilerlemesine sahip olabilir.

------------------------------------------------------------------------

## Courses → UserCourseProgress

``` text
Courses.Id
    │
    └────── UserCourseProgress.CourseId
```

İlişki:

``` text
1 : N
```

Bir kurs birçok kullanıcı tarafından takip edilebilir.

------------------------------------------------------------------------

## Courses → Units

``` text
Courses.Id
    │
    └────── Units.CourseId
```

İlişki:

``` text
1 : N
```

Bir kurs birçok ünite içerir.

------------------------------------------------------------------------

## Users → UserUnitProgress

``` text
Users.Id
    │
    └────── UserUnitProgress.UserId
```

İlişki:

``` text
1 : N
```

------------------------------------------------------------------------

## Units → UserUnitProgress

``` text
Units.Id
    │
    └────── UserUnitProgress.UnitId
```

İlişki:

``` text
1 : N
```

------------------------------------------------------------------------

# 11. Global Leaderboard

İlk etapta ayrıca `Leaderboard` tablosu oluşturmak gerekli değildir.

Sıralama UserStats üzerinden alınabilir:

``` sql
SELECT
    u.Id,
    u.Username,
    us.XP,
    us.Level,
    us.RankId
FROM Users u
INNER JOIN UserStats us
    ON u.Id = us.UserId
ORDER BY us.XP DESC;
```

Örnek sonuç:

    Sıra Username        XP   Level Rank
  ------ ---------- ------- ------- ----------
       1 Ahmet        12500      24 Diamond
       2 Mehmet       10800      21 Diamond
       3 Emirhan       9200      19 Platinum
       4 Ayşe          8700      18 Platinum

> Kullanıcı sayısı çok büyümediği sürece leaderboard'ı ayrıca fiziksel
> olarak saklamak gereksizdir. `ORDER BY XP DESC` ile hesaplanabilir.

------------------------------------------------------------------------

# 12. Önerilen Tam Yapı

``` text
Users
│
├── UserStats
│     │
│     └── Ranks
│
├── UserCourseProgress
│     │
│     └── Courses
│           │
│           └── LanguageLevels
│           │
│           └── Units
│
└── UserUnitProgress
      │
      └── Units
```

Daha görsel olarak:

``` text
                    ┌───────────────┐
                    │     Users     │
                    └───────┬───────┘
                            │
              ┌─────────────┼─────────────┐
              │             │             │
              ▼             ▼             ▼
       ┌────────────┐ ┌──────────────┐ ┌───────────────┐
       │ UserStats  │ │ UserCourse   │ │ UserUnit      │
       │            │ │ Progress     │ │ Progress      │
       └─────┬──────┘ └──────┬───────┘ └───────┬───────┘
             │               │                 │
             ▼               ▼                 │
        ┌─────────┐    ┌───────────┐            │
        │  Ranks  │    │  Courses  │◄───────────┘
        └─────────┘    └─────┬─────┘
                             │
                             ▼
                       ┌───────────┐
                       │   Units   │
                       └───────────┘
                             │
                             ▼
                     UserUnitProgress
```

------------------------------------------------------------------------

# 13. İleride Eklenecek Tablolar

Proje büyüdükçe aşağıdaki tabloları eklemek mantıklı olacaktır:

``` text
Questions
Words
Sentences
Topics
UserAnswers
UserWordProgress
UserSkillProgress
Achievements
UserAchievements
DailyChallenges
Streaks
Notifications
```

Özellikle senin AI destekli cümle/kelime üretim sistemin için ileride:

``` text
Courses
   ↓
Units
   ↓
Topics
   ↓
Words / Sentences / Questions
   ↓
UserAnswers
   ↓
UserSkillProgress
```

şeklinde bir yapı kurulabilir.

Bu sayede sistem:

> "Bu kullanıcı A2 seviyesinde ama özellikle Food topic'inde şu
> kelimelerde zayıf."

gibi bir bilgi çıkarabilir.

------------------------------------------------------------------------

# 14. Önemli Tasarım Kararları

### Users tablosunu küçük tut

``` text
Users
├── Kimlik
├── Email
├── PasswordHash
├── Role
└── CreatedAt
```

### İstatistikleri UserStats'e koy

``` text
UserStats
├── XP
├── Level
├── Rank
└── UpdatedAt
```

### Eğitim içeriğini kullanıcıdan bağımsız tut

``` text
Courses
└── Units
```

### Kullanıcının ilerlemesini ayrı tut

``` text
UserCourseProgress
UserUnitProgress
```

### Rank'i XP üzerinden hesapla

İlk sürümde:

``` text
XP → Rank
```

### Dil seviyesini Rank ile karıştırma

``` text
XP / Game Level / Rank
```

ve

``` text
English Level (A1/A2/B1...)
```

ayrı kavramlar olarak tutulmalı.

------------------------------------------------------------------------

# 15. İlk Sürüm İçin Önerilen Tablo Listesi

İlk etapta sadece şu tabloları oluşturmak yeterli:

``` text
1. Users
2. Roles
3. UserStats
4. Ranks
5. LanguageLevels
6. Courses
7. Units
8. UserCourseProgress
9. UserUnitProgress
```

Daha sonra soru/kelime sistemini bunların üzerine bağlayabilirsin.

Bu yapı, mevcut projenin ileride kullanıcı profili, leaderboard, kurs
sistemi, ünite ilerlemesi, XP, rank ve kişiselleştirilmiş öğrenme
özelliklerine genişlemesine uygun bir temel oluşturur.
