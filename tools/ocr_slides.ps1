# Скрипт: извлечение текста из PNG-слайдов через Windows OCR API
# Использование: powershell -ExecutionPolicy Bypass -File tools/ocr_slides.ps1 [путь_к_папке_или_файлу]
param(
    [string]$Target = "references/slides",
    [string]$OutputFile = ""
)

# UTF-8 для корректного вывода кириллицы
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8

Add-Type -AssemblyName System.Runtime.WindowsRuntime

# Загрузка WinRT типов
$null = [Windows.Storage.StorageFile, Windows.Storage, ContentType=WindowsRuntime]
$null = [Windows.Media.Ocr.OcrEngine, Windows.Foundation, ContentType=WindowsRuntime]
$null = [Windows.Graphics.Imaging.BitmapDecoder, Windows.Graphics, ContentType=WindowsRuntime]
$null = [Windows.Storage.Streams.RandomAccessStream, Windows.Storage.Streams, ContentType=WindowsRuntime]

# Helper для await WinRT async
$asTaskGeneric = ([System.WindowsRuntimeSystemExtensions].GetMethods() | Where-Object {
    $_.Name -eq 'AsTask' -and $_.GetParameters().Count -eq 1 -and
    $_.GetParameters()[0].ParameterType.Name -eq 'IAsyncOperation`1'
})[0]

function Await($WinRtTask, $ResultType) {
    $asTask = $asTaskGeneric.MakeGenericMethod($ResultType)
    $netTask = $asTask.Invoke($null, @($WinRtTask))
    $netTask.Wait(-1) | Out-Null
    $netTask.Result
}

# Получить OCR engine (русский + английский)
$langs = [Windows.Media.Ocr.OcrEngine]::AvailableRecognizerLanguages
$ruLang = $langs | Where-Object { $_.LanguageTag -eq 'ru' } | Select-Object -First 1
$enLang = $langs | Where-Object { $_.LanguageTag -eq 'en' } | Select-Object -First 1

if (-not $ruLang -and -not $enLang) {
    Write-Error "Нет доступных языков OCR"
    exit 1
}

$engine = [Windows.Media.Ocr.OcrEngine]::TryCreateFromLanguage($ruLang)
if (-not $engine) {
    $engine = [Windows.Media.Ocr.OcrEngine]::TryCreateFromLanguage($enLang)
}
if (-not $engine) {
    $engine = [Windows.Media.Ocr.OcrEngine]::TryCreateFromUserProfileLanguages()
}
if (-not $engine) {
    Write-Error "Не удалось создать OCR engine"
    exit 1
}

Write-Output "OCR engine: $($engine.RecognizerLanguage.LanguageTag)"

# Собрать все PNG файлы
$files = @()
if (Test-Path $Target -PathType Container) {
    $files = Get-ChildItem -Path $Target -Recurse -Filter *.png
} elseif (Test-Path $Target -PathType Leaf) {
    $files = @(Get-Item $Target)
} else {
    Write-Error "Путь не найден: $Target"
    exit 1
}

Write-Output "Найдено PNG: $($files.Count)"

# Буфер для записи в файл
$sb = New-Object System.Text.StringBuilder

foreach ($file in $files) {
    $header = "`n===== $($file.FullName) ====="
    Write-Output $header
    [void]$sb.AppendLine($header)
    try {
        # Открыть файл
        $storageFile = Await ([Windows.Storage.StorageFile]::GetFileFromPathAsync($file.FullName)) ([Windows.Storage.StorageFile])
        # Открыть поток
        $stream = Await ($storageFile.OpenAsync([Windows.Storage.FileAccessMode]::Read)) ([Windows.Storage.Streams.IRandomAccessStream])
        # Декодировать
        $decoder = Await ([Windows.Graphics.Imaging.BitmapDecoder]::CreateAsync($stream)) ([Windows.Graphics.Imaging.BitmapDecoder])
        # Получить SoftwareBitmap
        $softwareBitmap = Await ($decoder.GetSoftwareBitmapAsync()) ([Windows.Graphics.Imaging.SoftwareBitmap])
        # Распознать
        $result = Await ($engine.RecognizeAsync($softwareBitmap)) ([Windows.Media.Ocr.OcrResult])
        # Вывести текст
        if ($result.Text) {
            Write-Output $result.Text
            [void]$sb.AppendLine($result.Text)
        } else {
            Write-Output "(текст не распознан)"
            [void]$sb.AppendLine("(текст не распознан)")
        }
        $stream.Dispose()
    } catch {
        $err = "ОШИБКА: $_"
        Write-Output $err
        [void]$sb.AppendLine($err)
    }
}

# Запись в файл, если указан
if ($OutputFile) {
    $outPath = Join-Path (Get-Location) $OutputFile
    [System.IO.File]::WriteAllText($outPath, $sb.ToString(), [System.Text.Encoding]::UTF8)
    Write-Output "`nСохранено в: $outPath"
}
