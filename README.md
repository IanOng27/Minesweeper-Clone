# No-Guess Minesweeper (WinForms)

A fully-featured, desktop Minesweeper clone built in C# using Windows Forms. Unlike traditional Minesweeper games that can force players into frustrating 50/50 guessing situations at the end of a match, this engine guarantees **100% mathematically solvable board states** from your very first move.

---

## 🚀 Features

* **100% Guess-Free Boards:** Features a built-in virtual simulation solver that mathematically verifies layout solubility before launching a match.
* **🌓 Native Dark Mode Toggle:** Seamlessly switch between a classic light desktop layout and a modern dark aesthetic with high-visibility neon proximity indicators.
* **🕹️ Multiple Difficulty Presets:**
    * *Easy:* 8x8 Grid – 10 Mines (Perfect for fast warmups)
    * *Standard Hard:* 16x16 Grid – 40 Mines
    * *Expert No-Guess:* 16x30 Grid – 99 Mines
    * *😈 Complex Evil Mode:* 24x30 Grid – 130 Mines (Forces advanced pattern recognition)
* **⚡ Modern Gameplay Conveniences:** Supports standard right-click flagging, automatic zero-cell recursive cascade expansions, and double-click/chord sweeps.
* **📐 Auto-Centering Viewport:** The game matrix automatically center-aligns itself dynamically upon window resize sweeps or difficulty changes.

---

## 🧠 Architectural Overview & OOP Concepts

This project was built to practice clean **Object-Oriented Programming (OOP)** rules and efficient data structures within the .NET environment:

### 1. Parallel Matrix Architecture
The application separates its core business data logic from the visual UI layout loop by maintaining two identical multi-dimensional arrays:
* `int[,] backendBoard`: Manages structural cell states mapping empty spaces (`0`), mine configurations (`-1`), or proximity integers (`1-8`).
* `Button[,] gridButtons`: Holds runtime visual references to the actual UI interaction object components.

### 2. Recursive Zero-Cascades
When clicking an unmined tile holding a value of `0`, the application executes a recursive flood-fill algorithm via `RevealCell()` to cascade boundaries open automatically until hitting numbered shorelines, preventing redundant player clicks.

### 3. Event-Driven Inputs
The operational game loop is entirely event-driven. UI components listen for hardware interrupts via registered event hooks (`MouseDown`, `MouseUp`, and `Tick`), passing context variables through C# Lambda expressions `(s, e) => ...` dynamically.

---

## 🛠️ The Solver & Anti-50/50 Engine

The highlight of the backend layout generator is the `ValidateBoardWithPatternRules` simulation process. When generating a grid, an AI-proxy runs through three tiers of logical checking:

1.  **Tactic A (Basic Counting):** Validates trivial edge placements based on basic flag deductions.
2.  **Tactic B (Geometric Patterns):** Scans exposed boundaries for advanced, complex multi-cell logic sequences (e.g., `1-2`, `1-2-1`, and `1-2-2-1` layout reductions).
3.  **Tactic C (Global Fringe Subsets):** If stuck, the engine runs localized hypothesis testing, trying "what-if" branch states. If a configuration leads to a mathematical contradiction, it rules out the impossible choice. If an absolute coin-flip 50/50 block is identified, the board is discarded entirely and re-generated instantly.

---

## 📦 How To Build and Run

### Prerequisites
* [.NET SDK (8.0 or newer)](https://dotnet.microsoft.com/download)
* Visual Studio 2022 (with **.NET Desktop Development** workload installed) OR VS Code with C# Extensions.

### Compilation Steps
1. Clone this repository to your local computer workstation:
   ```bash
   git clone [https://github.com/IanOng27/No-Guess-Minesweeper.git](https://github.com/IanOng27/No-Guess-Minesweeper.git)ss
