<p align="center">
  <img src="docs/images/argus-banner.svg" alt="Argus Plate Reader: find the plate, read it, see every step" width="880">
</p>

<p align="center">🇬🇧 English · 🇹🇷 <a href="docs/README.tr.md">Türkçe</a></p>

<p align="center">
  <b>👁️ A WPF app that finds the licence plate in a car photo and reads it.</b><br>
  You can watch every step of the image processing, one by one.
</p>

<p align="center">
  <a href="https://github.com/NisanurBulut/LicensePlateRecognitionSystem/actions/workflows/build.yml"><img src="https://github.com/NisanurBulut/LicensePlateRecognitionSystem/actions/workflows/build.yml/badge.svg" alt="Build status"></a>
  <a href="https://github.com/NisanurBulut/LicensePlateRecognitionSystem/releases"><img src="https://img.shields.io/github/v/release/NisanurBulut/LicensePlateRecognitionSystem?color=00B4D8" alt="Latest release"></a>
  <img src="https://img.shields.io/badge/.NET_Framework-4.8-512BD4" alt=".NET Framework 4.8">
  <img src="https://img.shields.io/badge/WPF-MVVM-7B61FF" alt="WPF MVVM">
  <a href="PlakaTanimaSistemi/COPYING.txt"><img src="https://img.shields.io/badge/license-GPL--3.0-00B4D8" alt="GPL-3.0 license"></a>
</p>

<p align="center">
  ⭐ <b>Do you like Argus Plate Reader? Please give it a star!</b> ⭐<br>
  It is free and it only takes a second, but it helps me a lot.<br><br>
  <a href="https://github.com/NisanurBulut/LicensePlateRecognitionSystem"><img src="https://img.shields.io/github/stars/NisanurBulut/LicensePlateRecognitionSystem?style=social" alt="Star this repository on GitHub"></a>
</p>

<p align="center">
  <img src="docs/images/argus-demo.gif" alt="Argus Plate Reader: a sample photo, the 13 processing steps that appear one by one, and the plate that was found" width="880">
</p>

Argus Plate Reader takes a photo of the front of a car and looks for the licence plate.
It cleans the picture step by step, finds the shape of the plate, crops it and reads the letters and numbers.
At the end it checks that the text looks like a Turkish plate, for example `35 AF 8952`.

You see everything on one screen: the photo with a green box, the cropped plate, the text, some numbers and a card for each processing step.

> [!NOTE]
> The texts in the app are in Turkish. For example, the main button is called *Plakayı Tespit Et* (Find the plate).

This is the new version (WPF / MVVM) of the plate recognition project. The first WinForms version is still in this repository, in the `legacy` folder.

## ✨ What can it do?

| | Feature |
|---|---|
| 🔍 | **Step-by-step view**: 13 small cards show the picture after each step. Click a card to see it big. A light moves over the photo while the app works. |
| 🟩 | **Plate on the photo**: a green box shows where the plate is. The cropped plate and the text are shown next to it. |
| 🔤 | **Plate text (OCR)**: the app reads the letters and numbers and checks the Turkish format: city code (01–81), 1–3 letters, 2–4 numbers. |
| 📊 | **Numbers**: position, size, shape ratio, time and the number of regions that were found. |
| 🎨 | **Clear colours**: green for "plate found", yellow for "please check the text" or "plate not found", red for an error. |
| 🖱️ | **Easy to use**: drag a photo into the window, or use the buttons. Two sample photos are included. |
| ⌨️ | **Command line**: open a photo directly with `PlakaTanima.exe photo.jpg`. |
| ⚡ | **Smooth**: the work runs in the background, so the window never freezes. |

## 🖼️ Screens

<table>
  <tr>
    <td align="center"><b>✅ Result</b><br><img src="docs/images/argus-result.png" width="300" alt="Result screen: the photo with a green box, the cropped plate, the text and the numbers"></td>
    <td align="center"><b>🔍 One step, big</b><br><img src="docs/images/argus-pipeline.png" width="300" alt="Pipeline screen: the Otsu step is shown big and its card is selected"></td>
    <td align="center"><b>⚠️ Please check</b><br><img src="docs/images/argus-warning.png" width="300" alt="Warning screen: the plate was found but the text is not a valid Turkish plate"></td>
  </tr>
</table>

## 🚀 Quick start

You need Windows and Visual Studio with **.NET Framework 4.8** support.

1. Open `PlakaTanimaSistemi/PlakaTanima.sln`.
2. `PlakaTanima.Wpf` is the startup project.
3. Press **F5**. Click **Örnek 1** (Sample 1), then **Plakayı Tespit Et** (Find the plate).

Or use a terminal:

```bash
dotnet build PlakaTanimaSistemi/PlakaTanima.sln --configuration Release
PlakaTanimaSistemi/PlakaTanima.Wpf/bin/Release/net48/PlakaTanima.exe PlakaTanimaSistemi/images/1.jpg
```

> [!IMPORTANT]
> The project is built for **x86 (32-bit)**, because the Otsu and Tesseract libraries are 32-bit.

### 🔤 Reading the text (OCR)

Finding the plate works without any extra file. To also **read** the text, the app needs one language file.
It is not stored in this repository, because it is big.

1. Download [`tesseract-ocr-3.01.eng.tar.gz`](https://storage.googleapis.com/google-code-archive-downloads/v2/code.google.com/tesseract-ocr/tesseract-ocr-3.01.eng.tar.gz) (about 4 MB).
2. Take out only `eng.traineddata`:

   ```bash
   tar -xzf tesseract-ocr-3.01.eng.tar.gz tesseract-ocr/tessdata/eng.traineddata
   ```

3. Copy it to `PlakaTanimaSistemi/PlakaTanima.Wpf/tessdata/eng.traineddata`.
4. Build again.

> [!WARNING]
> Use version **3.01**. The OCR engine in this project is Tesseract 3.01. A newer language file (3.04 or later) makes it crash.
> The engine also needs the *Visual C++ 2008 runtime* on the computer.

If the file is missing, the app still finds the plate and tells you that it cannot read the characters.

## 🤔 How does it find a plate?

The app first makes the photo smaller (at most 480 × 360), then it cleans it and looks for a bright shape that is about 3 to 5.7 times wider than it is high. That is the shape of a plate.

```mermaid
flowchart LR
    A["Photo"] --> B["Resize<br/>max 480 x 360"]
    B --> C["Grey"]
    C --> D["Median filter"]
    D --> E["Sobel edges"]
    E --> F["Otsu threshold"]
    F --> G["Erosion"]
    G --> H["Dilation"]
    H --> I["Remove small blobs"]
    I --> J["Closing"]
    J --> K["Erosion"]
    K --> L["Keep big blobs"]
    L --> M{"Plate shape?<br/>width / height<br/>3 to 5.7"}
    M -- yes --> N["Crop the plate"]
    N --> O["OCR"]
    O --> P["Check Turkish<br/>plate format"]
    M -- no --> Q["No plate found"]
```

The [Turkish README](docs/README.tr.md) explains each step with pictures and code.

## 📐 Architecture

The code has three layers. `PlakaTanima.Core` has no window code, so it can be reused in another app.

| Layer | Colour | What does it do? |
|---|---|---|
| 🟪 **View** (`PlakaTanima.Wpf`: `MainWindow.xaml`, `App.xaml`) | purple | The screen: photo, result card, step cards, colours and animations. |
| 🟦 **ViewModel** (`PlakaTanima.Wpf/ViewModels`) | blue | The state of the screen and the commands. It starts the work in the background and fills the screen when the work is done. |
| 🟩 **Core** (`PlakaTanima.Core`) | green | `PlateRecognizer` finds the plate and `PlateTextReader` reads it. The image filters are in `ExtBitmap`. |

```mermaid
classDiagram
    direction LR

    class MainWindow {
        +Sample1 : string
        +Sample2 : string
    }
    class MainViewModel {
        +Steps : ObservableCollection~StepCardViewModel~
        +State : DetectionState
        +DisplayImage : BitmapSource
        +PlateImage : BitmapSource
        +RecognizedText : string
        +OcrNote : string
        +OpenCommand : ICommand
        +DetectCommand : ICommand
        +SampleCommand : ICommand
        +ShowResultCommand : ICommand
        +LoadImage(path) void
        +Shutdown() void
    }
    class ObservableObject {
        <<abstract>>
    }
    class RelayCommand
    class StepCardViewModel {
        +Index : int
        +Title : string
        +Description : string
        +Image : BitmapSource
    }
    class DetectionState {
        <<enumeration>>
        Idle
        Running
        Found
        NotFound
        Failed
    }
    class PlateRecognizer {
        +StepCount : int
        +Recognize(Bitmap original, IProgress~PipelineStep~ progress) PlateRecognitionResult
    }
    class PipelineStep {
        +Index : int
        +Total : int
        +Title : string
        +Description : string
        +Image : Bitmap
    }
    class PlateRecognitionResult {
        +Source : Bitmap
        +Annotated : Bitmap
        +PlateImage : Bitmap
        +BlobCount : int
        +ElapsedMilliseconds : long
        +Found : bool
    }
    class PlateCandidate {
        +Bounds : Rectangle
        +Area : int
        +AspectRatio : float
    }
    class PlateTextReader {
        +IsAvailable : bool
        +UnavailableReason : string
        +Read(Bitmap plate) PlateText
        +Dispose() void
    }
    class PlateText {
        +Text : string
        +Raw : string
        +IsValidFormat : bool
    }
    class ExtBitmap {
        <<static>>
        +MedianFilter(size) Bitmap
        +Sobel3x3Filter() Bitmap
        +DilateAndErodeFilter(size, type) Bitmap
        +CloseMorphologyFilter(size) Bitmap
    }

    MainWindow --> MainViewModel : DataContext
    MainViewModel --|> ObservableObject
    MainViewModel *-- StepCardViewModel : Steps
    MainViewModel --> DetectionState : State
    MainViewModel ..> RelayCommand : creates
    MainViewModel ..> PlateRecognizer : uses
    MainViewModel ..> PlateTextReader : uses
    StepCardViewModel ..> PipelineStep : built from
    PlateRecognizer ..> PipelineStep : reports
    PlateRecognizer --> PlateRecognitionResult : returns
    PlateRecognizer ..> ExtBitmap : uses
    PlateRecognitionResult o-- PlateCandidate : Candidates
    PlateTextReader --> PlateText : returns

    style MainWindow fill:#EDE9FF,stroke:#7B61FF,color:#0F172A
    style MainViewModel fill:#E0F7FC,stroke:#00B4D8,color:#0F172A
    style ObservableObject fill:#E0F7FC,stroke:#00B4D8,color:#0F172A
    style RelayCommand fill:#E0F7FC,stroke:#00B4D8,color:#0F172A
    style StepCardViewModel fill:#E0F7FC,stroke:#00B4D8,color:#0F172A
    style DetectionState fill:#E0F7FC,stroke:#00B4D8,color:#0F172A
    style PlateRecognizer fill:#DCFCE7,stroke:#22C55E,color:#0F172A
    style PipelineStep fill:#DCFCE7,stroke:#22C55E,color:#0F172A
    style PlateRecognitionResult fill:#DCFCE7,stroke:#22C55E,color:#0F172A
    style PlateCandidate fill:#DCFCE7,stroke:#22C55E,color:#0F172A
    style PlateTextReader fill:#DCFCE7,stroke:#22C55E,color:#0F172A
    style PlateText fill:#DCFCE7,stroke:#22C55E,color:#0F172A
    style ExtBitmap fill:#DCFCE7,stroke:#22C55E,color:#0F172A
```

This is what happens when you click the main button:

```mermaid
sequenceDiagram
    actor User
    participant UI as MainWindow
    participant VM as MainViewModel
    participant R as PlateRecognizer
    participant T as PlateTextReader

    User->>UI: Click the detect button
    UI->>VM: DetectCommand
    VM->>R: Recognize(photo, progress) on a background task
    loop 13 steps
        R-->>VM: PipelineStep with title and image
        VM-->>UI: Add a step card and move the progress bar
    end
    R-->>VM: PlateRecognitionResult
    VM->>T: Read(plate image)
    T-->>VM: PlateText
    VM-->>UI: Show the plate, the text and the numbers
```

## 📄 Report

This project also has a written report:

| | |
|---|---|
| **Document** | 📄 [Project report (PDF)](PlateRecognitionSystemReport.pdf) |

## 🏷️ Releases and versions

Work happens on branches. A branch reaches `master` with a **pull request** (a request to merge the branch).
When a pull request is merged, GitHub Actions makes a new **GitHub release** with a version number like `v1.2.3`.
The first version is `v0.0.0`, but this repository already has an old release called `v1.0.0.0`.
It is read as `1.0.0`, so the new versions go on from there.

The name of the branch decides which number goes up. The name only has to *contain* the word:

| Branch name contains | Number that goes up | Example | Label on the pull request |
|---|---|---|---|
| `major` | **major** (the other two go back to 0) | `v1.0.0` → `v2.0.0` | 🔴 `breaking` |
| `dev` | **minor** (the patch goes back to 0) | `v1.0.0` → `v1.1.0` | 🟡 `enhancement` |
| `hotfix` | **patch** | `v1.1.0` → `v1.1.1` | 🟠 `bug` |
| none of these words | **patch** | `v1.1.0` → `v1.1.1` | no label |

| Workflow | When does it run? | What does it do? |
|---|---|---|
| [`build.yml`](.github/workflows/build.yml) | A push to `master` and every pull request | Builds the app on Windows. The result can be downloaded for 7 days (an *artifact*). |
| [`auto-label.yml`](.github/workflows/auto-label.yml) | A pull request is opened | Puts the label from the table above on the pull request. It makes the label if it does not exist. |
| [`release.yml`](.github/workflows/release.yml) | A pull request is merged into `master` | Creates the next `vX.Y.Z` tag and the release notes. A direct push does not make a release. |
| [`auto-assign.yml`](.github/workflows/auto-assign.yml) | A pull request is opened | Assigns the pull request to `NisanurBulut`, also when a bot opened it. |

All releases are on the [**Releases**](https://github.com/NisanurBulut/LicensePlateRecognitionSystem/releases) page.

## 🗂️ Project structure

| Path | Content |
|---|---|
| `PlakaTanimaSistemi/PlakaTanima.Wpf` | **The main app** (WPF): screens, view model, colours, icon and animations. |
| `PlakaTanimaSistemi/PlakaTanima.Core` | The image processing and the plate reading code. No window code. |
| `PlakaTanimaSistemi/legacy/LicensePlateRecognition` | The first WinForms app. It finds the plate but does not read the text. Kept as a reference. |
| `PlakaTanimaSistemi/references` | Libraries: AForge, Tesseract and Otsu (all 32-bit). |
| `PlakaTanimaSistemi/images` | Two sample photos. |
| `docs/images` | Banner, logo, screenshots and the animation for this README. |
| `docs/README.tr.md` | The Turkish README, with the full text about the first version. |
| `tools` | Scripts that make the icon, the logo, the banner and the animation again. |
| `PlateRecognitionSystemReport.pdf` | The project report. |

> [!TIP]
> The project is called **Argus Plate Reader**. The folder names, the namespaces (`PlakaTanima.*`), the window title and the repository name (`LicensePlateRecognitionSystem`) have not changed.

<details>
<summary>🛠️ How to make the images again</summary>

```bash
powershell -STA -File tools/render-assets.ps1
dotnet build PlakaTanimaSistemi/PlakaTanima.sln --configuration Release
powershell -STA -File tools/record-demo.ps1
```

`render-assets.ps1` draws the app icon (`PlakaTanima.Wpf/Assets/app.ico`), the logo and the banner. They all come from the same shapes, so they always match.
`record-demo.ps1` opens the built app, clicks through the sample photos and saves the screenshots and the animation in `docs/images`.
Build first, and do not use the app while it records. Put the OCR language file in place before you record, or the plate text will not be in the pictures.

</details>

## 🌍 Turkish documentation

The Turkish text is in [docs/README.tr.md](docs/README.tr.md). It also explains the first WinForms version: the filters, the morphology steps and the plate search, with pictures.

## 📄 License

GPL-3.0, see [COPYING.txt](PlakaTanimaSistemi/COPYING.txt).

---

<p align="center">
  ⭐ <b>Made with a lot of filters and a little eye. If you like it, please <a href="https://github.com/NisanurBulut/LicensePlateRecognitionSystem">star the repository</a>!</b> ⭐
</p>
