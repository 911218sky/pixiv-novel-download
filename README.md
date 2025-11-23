# Pixiv Novel Downloader

A simple tool to download Pixiv novels as TXT files.

## ✨ Features

- 📚 Download single chapters or entire series
- 🚀 Fast multi-threaded downloads
- 🔐 Cookie-based authentication for private novels
- 📝 Clean UTF-8 text file output

## 🚀 Quick Start

### 1. Requirements

- Install [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- A Pixiv account

### 2. Get Your Pixiv Cookie

1. Log in to [Pixiv](https://www.pixiv.net/)
2. Press F12 to open Developer Tools
3. Go to "Application" → "Cookies"
4. Copy the value of `PHPSESSID`

### 3. Configuration

Copy the configuration template:

```bash
copy appsettings.example.jsonc appsettings.jsonc
```

Edit `appsettings.jsonc` and paste your cookie:

```json
{
  "Cookie": "your_PHPSESSID_value_here"
}
```

### 4. Run

**Windows:**
```bash
run.bat
```

**Other platforms:**
```bash
dotnet run
```

Enter a novel URL, for example:
- Series: `https://www.pixiv.net/novel/series/11713692`
- Single chapter: `https://www.pixiv.net/novel/show.php?id=26333534`

Downloaded files will be saved to the `data` folder.

## ⚙️ Configuration Options

| Parameter | Default | Description |
|-----------|---------|-------------|
| `Cookie` | - | Your Pixiv PHPSESSID cookie |
| `Concurrency` | 5 | Number of chapters to download simultaneously |
| `RequestDelayMs` | 1000 | Delay between requests (milliseconds) |
| `OutputDir` | `data` | Output folder |
| `DefaultTimeoutMs` | 15000 | Request timeout (milliseconds) |

## 💡 Troubleshooting

**Download fails or too slow?**
- Increase `RequestDelayMs` (e.g., to 2000)
- Decrease `Concurrency` (e.g., to 3)

**Empty output file?**
- Verify your cookie is correct
- Check if the novel is private or deleted

**Garbled text?**
- Open with a UTF-8 compatible editor (VS Code, Notepad++, etc.)

## 📄 License

This project is licensed under the GNU Lesser General Public License v3.0 (LGPL-3.0). See the [LICENSE](LICENSE) file for details.

