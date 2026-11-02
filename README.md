# BIOS Flasher - App Windows (.NET 7)

Aplikasi desktop sederhana (tampilan klasik ala Windows 2000) untuk
meng-upload file BIOS ke chip SPI-NOR lewat board **ESP32-C3-DevKitM-1**
(firmware: `SPI_ESP32C3_biosFLASHER`, USB native, tanpa USB-TTL).

## Download

EXE siap pakai (single file, **tidak perlu install .NET**):

> https://github.com/micropeltes/BiosFlasher/releases/latest

Mau build sendiri? Lihat bagian **Build** di bawah.

## Bahasa / Languages / 语言

Tersedia 3 bahasa, dipilih dari combo **Bahasa / Language / 语言** di kotak
"Koneksi" (berlaku langsung, tanpa restart):

| Bahasa | Contoh tombol utama |
|---|---|
| Bahasa Indonesia | TULIS KE CHIP |
| English | WRITE TO CHIP |
| 中文（简体） | 写入芯片 |

Pilihan disimpan di `%APPDATA%\BiosFlasher\settings.ini` (`lang=id|en|zh`).
Kalau file itu belum ada, bahasa mengikuti bahasa Windows (default Indonesia).
Saat bahasa Mandarin dipakai, font otomatis berganti ke *Microsoft YaHei UI*
supaya karakter Han tampil rapi.

## Fitur

| Fitur | Keterangan |
|---|---|
| Tulis BIOS | unprotect → hapus sector → **blank check** → tulis → verifikasi |
| Tulis biner cepat | perintah `wbin` 1024 byte/chunk (otomatis fallback ke hex untuk firmware lama) |
| Verifikasi CRC32 | CRC32 file vs chip (zlib/PNG), fallback byte-per-byte |
| Backup / Baca | simpan isi chip ke `.bin` sesuai kapasitas hasil `info` |
| Hapus chip | chip erase total (butuh konfirmasi) |
| Auto port | memilih sendiri port ESP32-C3 (USB VID `0x303A`) lewat registry |
| Drag & drop | lepas file `.bin` ke jendela |
| Log | tersimpan otomatis di `%APPDATA%\BiosFlasher\logs\` + tombol **Simpan Log** |
| 3 bahasa | Indonesia / English / 中文 (tersimpan di `settings.ini`) |
| Resizable | jendela bisa diperbesar/maksimal |
| CLI | `BiosFlasher.exe --cli write bios.bin -p COM5` |
| Progress | persen + byte + ETA per tahap |

## Cara pakai

1. Colok ESP32-C3 ke USB (port USB native, bukan UART/USB-TTL).
2. Jalankan `dist\BiosFlasher.exe` (hasil `build.bat`) atau
   `bin\Release\net7.0-windows\BiosFlasher.exe` (hasil `dotnet build`).
3. **Port USB** -> pilih port ESP32-C3 -> klik **Hubungkan**.
   Kalau port tidak muncul, klik **Muat Ulang**.
4. Chip akan dibaca otomatis: nama, JEDEC ID, kapasitas, status.
5. **Pilih File...** -> pilih file BIOS `.bin`.
6. **TULIS KE CHIP** -> konfirmasi -> proses: unprotect -> hapus sector ->
   tulis -> verifikasi. Progress bar + log di bawah.
7. **Backup / Baca** untuk menyimpan isi chip ke file (butuh Identifikasi dulu).
8. **Hapus Chip** = chip erase total. **Batal** menghentikan operasi.

## Isi project

| File | Keterangan |
|---|---|
| `Program.cs` | entry point; routing GUI vs `--cli`, AttachConsole untuk output CLI |
| `MainForm.cs` | seluruh UI, dirakit manual (tanpa designer) + ganti bahasa + drag&drop |
| `Lang.cs` | kamus 3 bahasa + simpan/baca `settings.ini` |
| `Protocol.cs` | klien serial: prompt `> `, ACK `+`, hex & biner (`wbin`), CRC32 |
| `ChipInfo.cs` | parser keluaran perintah `info` |
| `PortScanner.cs` | deteksi port ESP32-C3 (VID 0x303A) via registry |
| `Cli.cs` | mode command line (info/write/verify/backup/erase/freq) |
| `build.bat` | `dotnet publish` -> `dist\BiosFlasher.exe` (single file) |
| `.github/workflows/build.yml` | CI: build + publish artifact |

## Mode command line

```bat
BiosFlasher.exe --cli info
BiosFlasher.exe --cli write bios.bin -p COM5
BiosFlasher.exe --cli verify bios.bin
BiosFlasher.exe --cli backup backup.bin
BiosFlasher.exe --cli erase --yes
BiosFlasher.exe --cli freq 8000
BiosFlasher.exe --cli --help
```

Exit code: `0` sukses, `1` gagal, `2` argumen salah.

## Build

```bat
dotnet build -c Release          :: cepat, butuh .NET 7 Desktop Runtime
build.bat                        :: single-file self-contained di dist\
```

## Catatan

* Ukuran per baris data 256 byte (batas firmware), ACK `+` per baris.
* Write selalu diikuti verifikasi; kalau verifikasi gagal muncul
  `VERIFY GAGAL` dan baris `MISMATCH` di log.
* Perintah firmware yang dipakai: `info`, `unprotect`, `erase`,
  `write`, `verify`, `binread`, `chiperase confirm`, `freq`, `reset`.
* Diuji otomatis terhadap simulator firmware (21 pemeriksaan, semua lulus).
