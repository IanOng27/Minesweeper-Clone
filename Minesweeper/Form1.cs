// ==================================================================================
// This Minesweeper Clone is inspired by the one I usually play at minesweeper.online
// ==================================================================================

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace Minesweeper
{
    public partial class Form1 : Form
    {
        // ==========================================
        // GAME STATE & DEFAULT CONFIGURATIONS
        // ==========================================
        private int rows = 8;          // Default easy row count
        private int cols = 8;          // Default easy column count
        private int totalMines = 10;   // Default easy mine distribution total

        // ==========================================
        // LAYOUT & DIMENSION CONSTANTS
        // ==========================================
        private const int FIXED_BUTTON_SIZE = 35; // Grid buttons remain exactly 35x35 pixels for visual clarity
        private int topPanelHeight = 95;          // Reserved vertical pixel space for stats counters and menus
        private int padding = 10;                 // Uniform edge padding space wrapped around structural elements

        // ==========================================
        // WINDOWS FORMS USER INTERFACE ELEMENTS
        // ==========================================
        private MenuStrip menuStrip = new MenuStrip();              // Standard top drop-down operational selection bar
        private Button btnReset = new Button();                     // Status/Reset smiley-face interaction button
        private Label lblMineCounter = new Label();                 // Digital display container monitoring remaining hidden mines
        private Label lblTimer = new Label();                       // Digital stopwatch tracking operational gameplay seconds
        private System.Windows.Forms.Timer gameTimer = new System.Windows.Forms.Timer(); // System event ticker pulsing every 1000ms
        private Panel boardPanel = new Panel();                     // Dynamic container clipping and centering the grid buttons

        // ==========================================
        // BACKEND LOGICAL ARRAYS (Matrix Maps)
        // ==========================================
        private int[,] backendBoard = new int[0, 0];    // Value matrix: (-1 = Mine, 0 = Empty Space, 1-8 = Adjacent count)
        private Button[,] gridButtons = new Button[0, 0]; // Visual matrix matching the interface buttons to coordinates
        private bool[,] revealed = new bool[0, 0];      // Tracking matrix recording which coordinates have been left-clicked
        private bool[,] flagged = new bool[0, 0];       // Tracking matrix recording right-click flag placements

        // ==========================================
        // RUNTIME TRACKING VARIABLES
        // ==========================================
        private int flagsPlaced = 0;              // Integer accumulation tallying active board flags
        private int timeElapsed = 0;              // Total elapsed seconds capped safely below 999
        private bool firstClickTracked = false;   // Safety latch checking if the player initialized the board's generator
        private bool gameOver = false;            // Global loop lock preventing grid inputs once victory/defeat triggers

        private Point designatedStartTile = new Point(-1, -1); // Coordinates holding the pre-selected safe 'X' button


        /// Form Constructor - Runs first when the executable launches
        public Form1()
        {
            InitializeComponent();      // Native WinForms component rendering engine pass
            SetupMenu();                // Programmatically populates difficulty modes and navigation layouts
            SetupTopPanelControls();    // Generates, styles, and places the status readouts onto the main display

            // Allow manual resizing of the application window while preserving system icons
            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.MaximizeBox = true;
            this.MinimizeBox = true;

            // Subscribe window size modification events to our layout optimization logic
            this.Resize += Form1_Resize;

            // Run initialization sequence to generate the starting interface canvas
            StartNewGame();
        }

        /// Populates the drop-down difficulty select options at the top edge of the window
        private void SetupMenu()
        {
            ToolStripMenuItem gameMenu = new ToolStripMenuItem("Difficulty");

            // Define custom context buttons passing parameters through our ChangeDifficulty workflow
            ToolStripMenuItem easyItem = new ToolStripMenuItem("Easy No-Guess (8x8 - 10 Mines)", null, (s, e) => ChangeDifficulty(8, 8, 10));
            ToolStripMenuItem mediumItem = new ToolStripMenuItem("Medium No-Guess (16x16 - 40 Mines)", null, (s, e) => ChangeDifficulty(16, 16, 40));
            ToolStripMenuItem hardItem = new ToolStripMenuItem("Hard No-Guess (16x30 - 99 Mines)", null, (s, e) => ChangeDifficulty(16, 30, 99));
            ToolStripMenuItem evilItem = new ToolStripMenuItem("😈 Evil No-Guess (24x30 - 130 Mines)", null, (s, e) => ChangeDifficulty(24, 30, 130));

            // Attach difficulty entries as cascading items underneath the root parent menu entry
            gameMenu.DropDownItems.Add(easyItem);
            gameMenu.DropDownItems.Add(mediumItem);
            gameMenu.DropDownItems.Add(hardItem);
            gameMenu.DropDownItems.Add(evilItem);

            menuStrip.Items.Add(gameMenu);
            this.Controls.Add(menuStrip);
            this.MainMenuStrip = menuStrip; // Formally registers menu context hooks into active system assets
        }

        /// Switches global state variables to match selected parameters and launches a clean run
        private void ChangeDifficulty(int newRows, int newCols, int newMines)
        {
            rows = newRows;
            cols = newCols;
            totalMines = newMines;
            StartNewGame(); // Wipe assets and establish the fresh custom boundary matrix size
        }

        /// Styles and positions static top interface dashboard controls
        private void SetupTopPanelControls()
        {
            // Configure the Reset/Status button parameters
            btnReset.Size = new Size(50, 50);
            btnReset.Font = new Font("Segoe UI Emoji", 16, FontStyle.Bold);
            btnReset.Text = "🙂";
            btnReset.BackColor = Color.LightGray;
            btnReset.Click += (s, e) => StartNewGame(); // Left click restarts the app state using current boundaries
            this.Controls.Add(btnReset);

            // Configure the Mine Counter digital look and feel
            lblMineCounter.Size = new Size(60, 35);
            lblMineCounter.BackColor = Color.Black;
            lblMineCounter.ForeColor = Color.Red;
            lblMineCounter.Font = new Font("Consolas", 16, FontStyle.Bold);
            lblMineCounter.TextAlign = ContentAlignment.MiddleCenter;
            this.Controls.Add(lblMineCounter);

            // Configure the Stopwatch digital look and feel
            lblTimer.Size = new Size(60, 35);
            lblTimer.BackColor = Color.Black;
            lblTimer.ForeColor = Color.Red;
            lblTimer.Font = new Font("Consolas", 16, FontStyle.Bold);
            lblTimer.TextAlign = ContentAlignment.MiddleCenter;
            lblTimer.Text = "000";
            this.Controls.Add(lblTimer);

            // Establish the parent boundaries of the board panel viewport container
            boardPanel.Location = new Point(padding, topPanelHeight);
            this.Controls.Add(boardPanel);

            // Configure system ticker parameters to loop endlessly every 1000 milliseconds (1 second)
            gameTimer.Interval = 1000;
            gameTimer.Tick += GameTimer_Tick;
        }

        /// Performs full system reset and draws empty interaction grids
        private void StartNewGame()
        {
            gameTimer.Stop();     // Halt active runtime system stopwatch ticks
            timeElapsed = 0;      // Zero elapsed time counter asset
            flagsPlaced = 0;      // Zero user flag placement registry
            firstClickTracked = false; // Relock board map generator parameters
            gameOver = false;     // Clear execution freeze overrides

            lblTimer.Text = "000";
            btnReset.Text = "🙂";
            UpdateMineCounter();  // Push current standard mine totals to display

            boardPanel.Controls.Clear(); // Completely purge old WinForms control collection instances

            // Allocate exact multi-dimensional array sizes to process grid spaces safely without overflow
            backendBoard = new int[rows, cols];
            gridButtons = new Button[rows, cols];
            revealed = new bool[rows, cols];
            flagged = new bool[rows, cols];

            // Auto-calculate structural window display boundary sizes matching target difficulty grids
            int requiredClientWidth = (cols * FIXED_BUTTON_SIZE) + (padding * 2);
            int requiredClientHeight = (rows * FIXED_BUTTON_SIZE) + topPanelHeight + padding;

            this.ClientSize = new Size(requiredClientWidth, requiredClientHeight); // Adjust app frame width/height safely

            // Construct physical interaction button controls row-by-row
            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    Button btn = new Button();
                    btn.Size = new Size(FIXED_BUTTON_SIZE, FIXED_BUTTON_SIZE);
                    btn.Location = new Point(c * FIXED_BUTTON_SIZE, r * FIXED_BUTTON_SIZE);
                    btn.Font = new Font("Arial", 11, FontStyle.Bold);
                    btn.Tag = new Point(r, c); // Store programmatic coordinate details directly in spatial memory tag slots

                    // Route basic input clicks to functional unified event managers
                    btn.MouseDown += Button_MouseDown;
                    btn.MouseUp += Button_MouseUp;

                    boardPanel.Controls.Add(btn); // Mount button structure onto the viewing viewport asset
                    gridButtons[r, c] = btn;       // Store pointer in tracking array index
                }
            }

            // Pick a completely random start location coordinates for the opening move
            Random rand = new Random();
            int startR = rand.Next(0, rows);
            int startC = rand.Next(0, cols);
            designatedStartTile = new Point(startR, startC);

            PositionUIElements(); // Execute layout calculations to snap components cleanly to grid centerlines

            // Visual indicator marking exactly where the safe opening tile boundary is locked
            gridButtons[startR, startC].Text = "X";
            gridButtons[startR, startC].ForeColor = Color.Red;
        }

        /// Catches window adjustments and ensures components re-center smoothly
        private void Form1_Resize(object? sender, EventArgs e)
        {
            PositionUIElements();
        }

        /// Center dashboard text indicators and button clusters uniformly
        private void PositionUIElements()
        {
            if (gridButtons == null || gridButtons.Length == 0) return;

            // Anchor top panel tools dynamically along geometric calculations
            btnReset.Location = new Point((this.ClientSize.Width - btnReset.Width) / 2, 35);
            lblMineCounter.Location = new Point(15, 43);
            lblTimer.Location = new Point(this.ClientSize.Width - lblTimer.Width - 15, 43);

            // Automatically resize the inner clipping panel asset container
            boardPanel.Size = new Size(this.ClientSize.Width - (padding * 2), this.ClientSize.Height - topPanelHeight - padding);

            // Compute precise offset spacing variables to keep the game board perfectly centered 
            int offsetX = Math.Max(0, (boardPanel.Width - (cols * FIXED_BUTTON_SIZE)) / 2);
            int offsetY = Math.Max(0, (boardPanel.Height - (rows * FIXED_BUTTON_SIZE)) / 2);

            // Reposition button coordinates matching structural panel sizing shifts
            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    Button btn = gridButtons[r, c];
                    if (btn != null)
                    {
                        btn.Location = new Point(offsetX + (c * FIXED_BUTTON_SIZE), offsetY + (r * FIXED_BUTTON_SIZE));
                    }
                }
            }
        }

        /// Generates a complex board map layout free of guessing logic traps
        private void GeneratePlayableBoard(int startRow, int startCol)
        {
            int attempts = 0;
            Random rand = new Random();
            List<Point> existingMines = new List<Point>();

            // Identify if the active run requires complex Evil Mode behavior rulesets
            bool isEvilMode = (rows == 24 && cols == 30 && totalMines == 130);

            // Global retry loop: Continuously create and test boards until one satisfies our logic engine parameters
            while (attempts < 20000)
            {
                attempts++;
                Array.Clear(backendBoard, 0, backendBoard.Length); // Reset data map structure arrays
                existingMines.Clear();

                int placedMines = 0;
                // If in Evil Mode, place only 65% randomly. The rest are clustered to force advanced patterns.
                int uniformMinesCount = isEvilMode ? (int)(totalMines * 0.65) : totalMines;

                // Pass 1: Global distribution pass
                while (placedMines < uniformMinesCount)
                {
                    int r = rand.Next(0, rows);
                    int c = rand.Next(0, cols);

                    // Skip creation if within the 3x3 initial click zone around startRow/startCol
                    if (Math.Abs(r - startRow) <= 1 && Math.Abs(c - startCol) <= 1)
                        continue;

                    if (backendBoard[r, c] != -1)
                    {
                        backendBoard[r, c] = -1; // -1 represents an active mine index slot
                        existingMines.Add(new Point(r, c));
                        placedMines++;
                    }
                }

                // Pass 2: Clustered distribution pass (Evil mode specific pattern injection)
                while (placedMines < totalMines && existingMines.Count > 0)
                {
                    // Select an existing mine to use as a structural anchor point
                    Point parentMine = existingMines[rand.Next(0, existingMines.Count)];
                    int nr = parentMine.X + rand.Next(-1, 2); // Pick a neighbor row
                    int nc = parentMine.Y + rand.Next(-1, 2); // Pick a neighbor column

                    if (!IsValid(nr, nc) || backendBoard[nr, nc] == -1) continue;
                    if (Math.Abs(nr - startRow) <= 1 && Math.Abs(nc - startCol) <= 1) continue; // Respect initial safety zone

                    backendBoard[nr, nc] = -1;
                    existingMines.Add(new Point(nr, nc));
                    placedMines++;
                }

                // Pass 3: Process the final numeric counts for all remaining safe tiles
                for (int r = 0; r < rows; r++)
                {
                    for (int c = 0; c < cols; c++)
                    {
                        if (backendBoard[r, c] == -1) continue;
                        backendBoard[r, c] = CountAdjacentMines(backendBoard, r, c);
                    }
                }

                // Pass 4: Run the solver logic pass to ensure the board is 100% solvable without guessing
                if (ValidateBoardWithPatternRules(startRow, startCol, isEvilMode))
                {
                    break; // Solvable map locked in safely. Exit the generator routine loop.
                }
            }
        }

        /// Simulation Solver Engine: Replicates human deduction routines to verify board safety constraints
        private bool ValidateBoardWithPatternRules(int startRow, int startCol, bool filterForHighComplexity)
        {
            // Virtual validation state maps tracking simulated progression steps
            bool[,] vRevealed = new bool[rows, cols];
            bool[,] vFlagged = new bool[rows, cols];
            int safeCellsCount = (rows * cols) - totalMines;
            int cellsSolved = 0;

            // Clear out initial safe zone starting coordinates via flood fill cascade simulation
            Queue<Point> openQueue = new Queue<Point>();
            openQueue.Enqueue(new Point(startRow, startCol));

            while (openQueue.Count > 0)
            {
                Point p = openQueue.Dequeue();
                if (vRevealed[p.X, p.Y]) continue;

                vRevealed[p.X, p.Y] = true;
                cellsSolved++;

                // If the tile value is 0, add all surrounding tiles to the reveal queue
                if (backendBoard[p.X, p.Y] == 0)
                {
                    for (int i = -1; i <= 1; i++)
                    {
                        for (int j = -1; j <= 1; j++)
                        {
                            int nr = p.X + i; int nc = p.Y + j;
                            if (IsValid(nr, nc) && !vRevealed[nr, nc]) openQueue.Enqueue(new Point(nr, nc));
                        }
                    }
                }
            }

            bool progressMade = true;
            bool patternLogicTriggered = false; // Flag to verify if complex deductions were required

            // Primary operational deduction processing loops
            while (progressMade && cellsSolved < safeCellsCount)
            {
                progressMade = false;

                // ------------------------------------------
                // TACTIC A: STANDARD BASIC COUNTING DEDUCTIONS (B1 & B2)
                // ------------------------------------------
                for (int r = 0; r < rows; r++)
                {
                    for (int c = 0; c < cols; c++)
                    {
                        if (!vRevealed[r, c] || backendBoard[r, c] <= 0) continue;

                        int hiddenCount = 0;
                        int flaggedCount = 0;
                        List<Point> hiddenNeighbors = new List<Point>();

                        // Scan the surrounding 3x3 neighborhood area
                        for (int i = -1; i <= 1; i++)
                        {
                            for (int j = -1; j <= 1; j++)
                            {
                                int nr = r + i; int nc = c + j;
                                if (IsValid(nr, nc))
                                {
                                    if (vFlagged[nr, nc]) flaggedCount++;
                                    else if (!vRevealed[nr, nc])
                                    {
                                        hiddenCount++;
                                        hiddenNeighbors.Add(new Point(nr, nc));
                                    }
                                }
                            }
                        }

                        // Basic Safe Rule (B2): If current flags equal the number value, all other unrevealed neighbors are safe
                        if (flaggedCount == backendBoard[r, c] && hiddenCount > 0)
                        {
                            foreach (var p in hiddenNeighbors)
                            {
                                if (!vRevealed[p.X, p.Y])
                                {
                                    ExecuteSolverCascade(p, vRevealed, ref cellsSolved);
                                    progressMade = true;
                                }
                            }
                        }

                        // Basic Mine Rule (B1): If total available spaces equals the remaining mine count, they must all be mines
                        if (hiddenCount + flaggedCount == backendBoard[r, c] && hiddenCount > 0)
                        {
                            foreach (var p in hiddenNeighbors)
                            {
                                vFlagged[p.X, p.Y] = true;
                            }
                            progressMade = true;
                        }
                    }
                }

                if (progressMade) continue; // Run basic counting steps first before trying complex patterns

                // ------------------------------------------
                // TACTIC B: GEOMETRIC PATTERN RECOGNITION (Minesweeper.online Ruleset)
                // ------------------------------------------
                for (int r = 0; r < rows; r++)
                {
                    for (int c = 0; c < cols; c++)
                    {
                        if (!vRevealed[r, c] || backendBoard[r, c] <= 0) continue;

                        // HORIZONTAL SEQUENCE CHECKING
                        if (c < cols - 1 && vRevealed[r, c + 1] && backendBoard[r, c + 1] > 0)
                        {
                            // Reduce high numeric counts using adjacent flag counts (Reduction Rule)
                            int effectiveVal1 = GetEffectiveValue(r, c, vFlagged);
                            int effectiveVal2 = GetEffectiveValue(r, c + 1, vFlagged);

                            // Match for 1-2 Pattern variants
                            if (effectiveVal1 == 1 && effectiveVal2 == 2)
                            {
                                if (Apply12PatternHorizontal(r, c, vRevealed, vFlagged, ref cellsSolved))
                                {
                                    progressMade = true; patternLogicTriggered = true;
                                }
                            }

                            // Match for 1-2-1 Pattern variants
                            if (c < cols - 2 && vRevealed[r, c + 2] && backendBoard[r, c + 2] > 0)
                            {
                                int effectiveVal3 = GetEffectiveValue(r, c + 2, vFlagged);
                                if (effectiveVal1 == 1 && effectiveVal2 == 2 && effectiveVal3 == 1)
                                {
                                    if (Apply121PatternHorizontal(r, c, vRevealed, vFlagged, ref cellsSolved))
                                    {
                                        progressMade = true; patternLogicTriggered = true;
                                    }
                                }

                                // Match for 1-2-2-1 Pattern variants
                                if (c < cols - 3 && vRevealed[r, c + 3] && backendBoard[r, c + 3] > 0)
                                {
                                    int effectiveVal4 = GetEffectiveValue(r, c + 3, vFlagged);
                                    if (effectiveVal1 == 1 && effectiveVal2 == 2 && effectiveVal3 == 2 && effectiveVal4 == 1)
                                    {
                                        if (Apply1221PatternHorizontal(r, c, vRevealed, vFlagged, ref cellsSolved))
                                        {
                                            progressMade = true; patternLogicTriggered = true;
                                        }
                                    }
                                }
                            }
                        }

                        // VERTICAL SEQUENCE CHECKING
                        if (r < rows - 1 && vRevealed[r + 1, c] && backendBoard[r + 1, c] > 0)
                        {
                            int effectiveVal1 = GetEffectiveValue(r, c, vFlagged);
                            int effectiveVal2 = GetEffectiveValue(r + 1, c, vFlagged);

                            if (effectiveVal1 == 1 && effectiveVal2 == 2)
                            {
                                if (Apply12PatternVertical(r, c, vRevealed, vFlagged, ref cellsSolved))
                                {
                                    progressMade = true; patternLogicTriggered = true;
                                }
                            }

                            if (r < rows - 2 && vRevealed[r + 2, c] && backendBoard[r + 2, c] > 0)
                            {
                                int effectiveVal3 = GetEffectiveValue(r + 2, c, vFlagged);
                                if (effectiveVal1 == 1 && effectiveVal2 == 2 && effectiveVal3 == 1)
                                {
                                    if (Apply121PatternVertical(r, c, vRevealed, vFlagged, ref cellsSolved))
                                    {
                                        progressMade = true; patternLogicTriggered = true;
                                    }
                                }

                                if (r < rows - 3 && vRevealed[r + 3, c] && backendBoard[r + 3, c] > 0)
                                {
                                    int effectiveVal4 = GetEffectiveValue(r + 3, c, vFlagged);
                                    if (effectiveVal1 == 1 && effectiveVal2 == 2 && effectiveVal3 == 2 && effectiveVal4 == 1)
                                    {
                                        if (Apply1221PatternVertical(r, c, vRevealed, vFlagged, ref cellsSolved))
                                        {
                                            progressMade = true; patternLogicTriggered = true;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                if (progressMade) continue;

                // ------------------------------------------
                // TACTIC C: GLOBAL SET-CONSISTENCY PASS (ANTI-50/50 GUARANTEE)
                // ------------------------------------------
                // If counting and pattern deductions both stall, run an exhaustive simulation check on boundary tiles
                if (cellsSolved < safeCellsCount)
                {
                    if (AnalyzeFringeSubsets(vRevealed, vFlagged, ref cellsSolved))
                    {
                        progressMade = true;
                        patternLogicTriggered = true;
                    }
                }
            }

            // CRUCIAL FOR EVIL MODE: If a map is mathematically logical but was solvable using 
            // basic counting routines alone, reject it. This forces the generator to look for high-complexity boards.
            if (filterForHighComplexity && !patternLogicTriggered)
            {
                return false;
            }

            // Return true only if the simulated path successfully cleared every single safe cell
            return cellsSolved == safeCellsCount;
        }

        /// Anti-50/50 Core Engine: Tests empty boundary tiles for mathematical contradictions
        private bool AnalyzeFringeSubsets(bool[,] vRevealed, bool[,] vFlagged, ref int cellsSolved)
        {
            List<Point> frontiers = new List<Point>();
            // Gather all unrevealed, unflagged tiles that touch at least one visible number
            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    if (!vRevealed[r, c] && !vFlagged[r, c] && IsAdjacentToRevealedNumber(r, c, vRevealed))
                    {
                        frontiers.Add(new Point(r, c));
                    }
                }
            }

            // Performance Cap: Only process if the isolated pocket is small enough to evaluate efficiently
            if (frontiers.Count == 0 || frontiers.Count > 15) return false;

            foreach (var testTile in frontiers)
            {
                // Hypothesis 1: Simulate what happens if this tile contains a mine
                bool mineValid = TestHypothesis(testTile, true, frontiers, vRevealed, vFlagged);
                // Hypothesis 2: Simulate what happens if this tile is perfectly safe
                bool safeValid = TestHypothesis(testTile, false, frontiers, vRevealed, vFlagged);

                // EXPLICIT 50/50 DETECTION: If both choices are completely valid and create zero contradictions,
                // then this layout is an unresolvable coin-flip. Reject the map immediately.
                if (mineValid && safeValid)
                {
                    return false;
                }

                // If placing a mine breaks the board logic, then this tile MUST be safe!
                if (!mineValid && safeValid)
                {
                    ExecuteSolverCascade(testTile, vRevealed, ref cellsSolved);
                    return true;
                }
                // If treating the tile as safe breaks the board logic, then this tile MUST be a mine!
                if (mineValid && !safeValid)
                {
                    vFlagged[testTile.X, testTile.Y] = true;
                    return true;
                }
            }

            return false;
        }

        /// Runs a localized forward-checking pass under an assumed tile condition to look for layout errors
        private bool TestHypothesis(Point testPt, bool assumeMine, List<Point> scope, bool[,] origRev, bool[,] origFlg)
        {
            // Clone the active simulation state arrays to isolate testing modifications safely
            bool[,] simRev = (bool[,])origRev.Clone();
            bool[,] simFlg = (bool[,])origFlg.Clone();

            if (assumeMine) simFlg[testPt.X, testPt.Y] = true;
            else simRev[testPt.X, testPt.Y] = true;

            bool loop = true;
            while (loop)
            {
                loop = false;
                foreach (var pt in scope)
                {
                    if (simRev[pt.X, pt.Y] || simFlg[pt.X, pt.Y]) continue;

                    // Evaluate neighbor spaces against standard numeric values
                    for (int i = -1; i <= 1; i++)
                    {
                        for (int j = -1; j <= 1; j++)
                        {
                            int nr = pt.X + i; int nc = pt.Y + j;
                            if (!IsValid(nr, nc) || !simRev[nr, nc] || backendBoard[nr, nc] <= 0) continue;

                            int hCount = 0; int fCount = 0;
                            List<Point> hList = new List<Point>();

                            // Check constraints in the target tile's immediate neighborhood area
                            for (int x = -1; x <= 1; x++)
                            {
                                for (int y = -1; y <= 1; y++)
                                {
                                    int nnr = nr + x; int nnc = nc + y;
                                    if (IsValid(nnr, nnc))
                                    {
                                        if (simFlg[nnr, nnc]) fCount++;
                                        else if (!simRev[nnr, nnc]) { hCount++; hList.Add(new Point(nnr, nnc)); }
                                    }
                                }
                            }

                            // CONTRADICTION DETECTED: Too many flags assigned to this number position
                            if (fCount > backendBoard[nr, nc]) return false;
                            // CONTRADICTION DETECTED: Not enough empty spaces left to satisfy this number value
                            if (fCount + hCount < backendBoard[nr, nc]) return false;

                            // Cascade deductions within our hypothetical branch state
                            if (fCount == backendBoard[nr, nc] && hCount > 0)
                            {
                                foreach (var h in hList) simRev[h.X, h.Y] = true;
                                loop = true;
                            }
                            if (fCount + hCount == backendBoard[nr, nc] && hCount > 0)
                            {
                                foreach (var h in hList) simFlg[h.X, h.Y] = true;
                                loop = true;
                            }
                        }
                    }
                }
            }
            return true; // The current hypothetical branch layout contains no architectural logic flaws
        }

        /// Simple bounds checking asset ensuring coordinates exist inside the active array indexes
        private bool IsAdjacentToRevealedNumber(int r, int c, bool[,] vRevealed)
        {
            for (int i = -1; i <= 1; i++)
            {
                for (int j = -1; j <= 1; j++)
                {
                    if (IsValid(r + i, c + j) && vRevealed[r + i, c + j] && backendBoard[r + i, c + j] > 0)
                        return true;
                }
            }
            return false;
        }

        /// Computes effective pattern values by subtracting known flags from a tile's base value (Reduction Rule)
        private int GetEffectiveValue(int r, int c, bool[,] vFlagged)
        {
            int baseVal = backendBoard[r, c];
            for (int i = -1; i <= 1; i++)
            {
                for (int j = -1; j <= 1; j++)
                {
                    if (IsValid(r + i, c + j) && vFlagged[r + i, c + j])
                        baseVal--;
                }
            }
            return baseVal;
        }

        // =========================================================================
        // HORIZONTAL PATTERN DEDUCTIONS
        // =========================================================================
        private bool Apply12PatternHorizontal(int r, int c, bool[,] vRevealed, bool[,] vFlagged, ref int cellsSolved)
        {
            bool modified = false;
            int[] rowOffsets = { -1, 1 }; // Scan the rows immediately above and below our number line interface
            foreach (int ro in rowOffsets)
            {
                int targetRow = r + ro;
                if (!IsValid(targetRow, c) || !IsValid(targetRow, c + 1)) continue;

                // Verify there is a flat wall interface of unrevealed tiles present
                if (!vRevealed[targetRow, c] && !vRevealed[targetRow, c + 1])
                {
                    int c3 = c + 2; // Identify the third layout square directly flanking the 2 position
                    if (IsValid(targetRow, c3) && !vRevealed[targetRow, c3] && !vFlagged[targetRow, c3])
                    {
                        vFlagged[targetRow, c3] = true; // The 1-2 rule states this location must be a mine
                        modified = true;
                    }
                }
            }
            return modified;
        }

        private bool Apply121PatternHorizontal(int r, int c, bool[,] vRevealed, bool[,] vFlagged, ref int cellsSolved)
        {
            bool modified = false;
            int[] rowOffsets = { -1, 1 };
            foreach (int ro in rowOffsets)
            {
                int targetRow = r + ro;
                if (!IsValid(targetRow, c) || !IsValid(targetRow, c + 1) || !IsValid(targetRow, c + 2)) continue;

                if (!vRevealed[targetRow, c] && !vRevealed[targetRow, c + 1] && !vRevealed[targetRow, c + 2])
                {
                    // 1-2-1 configurations force mines under both 1 positions and guarantee the center 2 slot is safe
                    if (!vFlagged[targetRow, c]) { vFlagged[targetRow, c] = true; modified = true; }
                    if (!vFlagged[targetRow, c + 2]) { vFlagged[targetRow, c + 2] = true; modified = true; }
                    if (!vRevealed[targetRow, c + 1]) { ExecuteSolverCascade(new Point(targetRow, c + 1), vRevealed, ref cellsSolved); modified = true; }
                }
            }
            return modified;
        }

        private bool Apply1221PatternHorizontal(int r, int c, bool[,] vRevealed, bool[,] vFlagged, ref int cellsSolved)
        {
            bool modified = false;
            int[] rowOffsets = { -1, 1 };
            foreach (int ro in rowOffsets)
            {
                int targetRow = r + ro;
                if (!IsValid(targetRow, c) || !IsValid(targetRow, c + 1) || !IsValid(targetRow, c + 2) || !IsValid(targetRow, c + 3)) continue;

                if (!vRevealed[targetRow, c] && !vRevealed[targetRow, c + 1] && !vRevealed[targetRow, c + 2] && !vRevealed[targetRow, c + 3])
                {
                    // 1-2-2-1 configurations place mines under both 2s and clear the spaces flanking the 1s
                    if (!vFlagged[targetRow, c + 1]) { vFlagged[targetRow, c + 1] = true; modified = true; }
                    if (!vFlagged[targetRow, c + 2]) { vFlagged[targetRow, c + 2] = true; modified = true; }
                    if (!vRevealed[targetRow, c]) { ExecuteSolverCascade(new Point(targetRow, c), vRevealed, ref cellsSolved); modified = true; }
                    if (!vRevealed[targetRow, c + 3]) { ExecuteSolverCascade(new Point(targetRow, c + 3), vRevealed, ref cellsSolved); modified = true; }
                }
            }
            return modified;
        }

        // =========================================================================
        // VERTICAL PATTERN DEDUCTIONS
        // =========================================================================
        private bool Apply12PatternVertical(int r, int c, bool[,] vRevealed, bool[,] vFlagged, ref int cellsSolved)
        {
            bool modified = false;
            int[] colOffsets = { -1, 1 }; // Scan the columns immediately left and right of our vertical number line interface
            foreach (int co in colOffsets)
            {
                int targetCol = c + co;
                if (!IsValid(r, targetCol) || !IsValid(r + 1, targetCol)) continue;

                if (!vRevealed[r, targetCol] && !vRevealed[r + 1, targetCol])
                {
                    int r3 = r + 2; // Identify the third layout square directly flanking the bottom 2 position
                    if (IsValid(r3, targetCol) && !vRevealed[r3, targetCol] && !vFlagged[r3, targetCol])
                    {
                        vFlagged[r3, targetCol] = true;
                        modified = true;
                    }
                }
            }
            return modified;
        }

        private bool Apply121PatternVertical(int r, int c, bool[,] vRevealed, bool[,] vFlagged, ref int cellsSolved)
        {
            bool modified = false;
            int[] colOffsets = { -1, 1 };
            foreach (int co in colOffsets)
            {
                int targetCol = c + co;
                if (!IsValid(r, targetCol) || !IsValid(r + 1, targetCol) || !IsValid(r + 2, targetCol)) continue;

                if (!vRevealed[r, targetCol] && !vRevealed[r + 1, targetCol] && !vRevealed[r + 2, targetCol])
                {
                    if (!vFlagged[r, targetCol]) { vFlagged[r, targetCol] = true; modified = true; }
                    if (!vFlagged[r + 2, targetCol]) { vFlagged[r + 2, targetCol] = true; modified = true; }
                    if (!vRevealed[r + 1, targetCol]) { ExecuteSolverCascade(new Point(r + 1, targetCol), vRevealed, ref cellsSolved); modified = true; }
                }
            }
            return modified;
        }

        private bool Apply1221PatternVertical(int r, int c, bool[,] vRevealed, bool[,] vFlagged, ref int cellsSolved)
        {
            bool modified = false;
            int[] colOffsets = { -1, 1 };
            foreach (int co in colOffsets)
            {
                int targetCol = c + co;
                if (!IsValid(r, targetCol) || !IsValid(r + 1, targetCol) || !IsValid(r + 2, targetCol) || !IsValid(r + 3, targetCol)) continue;

                if (!vRevealed[r, targetCol] && !vRevealed[r + 1, targetCol] && !vRevealed[r + 2, targetCol] && !vRevealed[r + 3, targetCol])
                {
                    if (!vFlagged[r + 1, targetCol]) { vFlagged[r + 1, targetCol] = true; modified = true; }
                    if (!vFlagged[r + 2, targetCol]) { vFlagged[r + 2, targetCol] = true; modified = true; }
                    if (!vRevealed[r, targetCol]) { ExecuteSolverCascade(new Point(r, targetCol), vRevealed, ref cellsSolved); modified = true; }
                    if (!vRevealed[r + 3, targetCol]) { ExecuteSolverCascade(new Point(r + 3, targetCol), vRevealed, ref cellsSolved); modified = true; }
                }
            }
            return modified;
        }

        /// Solver utility technique: Mimics a recursive open cascade run inside solver tracking bounds
        private void ExecuteSolverCascade(Point targetPoint, bool[,] vRevealed, ref int cellsSolved)
        {
            Queue<Point> cascade = new Queue<Point>();
            cascade.Enqueue(targetPoint);
            while (cascade.Count > 0)
            {
                Point curr = cascade.Dequeue();
                if (vRevealed[curr.X, curr.Y]) continue;
                vRevealed[curr.X, curr.Y] = true;
                cellsSolved++;
                if (backendBoard[curr.X, curr.Y] == 0)
                {
                    for (int i = -1; i <= 1; i++)
                    {
                        for (int j = -1; j <= 1; j++)
                        {
                            int nr = curr.X + i; int nc = curr.Y + j;
                            if (IsValid(nr, nc) && !vRevealed[nr, nc]) cascade.Enqueue(new Point(nr, nc));
                        }
                    }
                }
            }
        }

        /// Simple loops counting physical target mine items embedded in a 3x3 surrounding radius
        private int CountAdjacentMines(int[,] board, int r, int c)
        {
            int count = 0;
            for (int i = -1; i <= 1; i++)
            {
                for (int j = -1; j <= 1; j++)
                {
                    if (IsValid(r + i, c + j) && board[r + i, c + j] == -1)
                        count++;
                }
            }
            return count;
        }

        /// Bounds checker matrix helper
        private bool IsValid(int r, int c)
        {
            return r >= 0 && r < rows && c >= 0 && c < cols;
        }

        /// Calculates flag offset tracking values and formats output numbers to fit standard look parameters
        private void UpdateMineCounter()
        {
            int displayCount = totalMines - flagsPlaced;
            lblMineCounter.Text = displayCount >= 0 ? displayCount.ToString("D3") : displayCount.ToString();
        }

        /// System timer ticks: Updates the visual display clock every passing second
        private void GameTimer_Tick(object? sender, EventArgs e)
        {
            if (timeElapsed < 999)
            {
                timeElapsed++;
                lblTimer.Text = timeElapsed.ToString("D3");
            }
        }

        /// MouseDown Event: Changes face emoji icon state into a surprised look when a player left-clicks a tile
        private void Button_MouseDown(object? sender, MouseEventArgs e)
        {
            if (gameOver || sender == null) return;

            if (e.Button == MouseButtons.Left)
            {
                Button clickedButton = (Button)sender;
                if (clickedButton.Tag is Point pos)
                {
                    if (!revealed[pos.X, pos.Y] && !flagged[pos.X, pos.Y])
                    {
                        btnReset.Text = "😮"; // Visual feedback mimicking original game engines
                    }
                }
            }
        }

        /// MouseUp Event: Primary action dispatcher translating user actions into grid updates
        private void Button_MouseUp(object? sender, MouseEventArgs e)
        {
            if (gameOver || sender == null) return;

            btnReset.Text = "🙂"; // Restore normal face status state immediately upon click release

            Button clickedButton = (Button)sender;
            if (clickedButton.Tag is Point pos)
            {
                int r = pos.X;
                int c = pos.Y;

                // Chord Action: Left-clicking an already revealed number runs a shorthand sweep sweep
                if (e.Button == MouseButtons.Left && revealed[r, c])
                {
                    TriggerChordSweep(r, c);
                    return;
                }

                if (revealed[r, c]) return; // Block input actions if tile is open already

                // FIRST MOVE SAFETY GENERATOR MECHANIC
                if (!firstClickTracked && e.Button == MouseButtons.Left)
                {
                    // Restrict initialization actions unless user interacts with the designated 'X' boundary
                    if (r != designatedStartTile.X || c != designatedStartTile.Y)
                    {
                        MessageBox.Show("Please click the pre-marked 'X' tile to start safely!", "Starting Point Required");
                        return;
                    }

                    firstClickTracked = true;
                    GeneratePlayableBoard(r, c); // Generate full board layout with safety zone locked around this spot
                    gameTimer.Start();           // Fire up the timer stopwatch engine
                }

                // RIGHT-CLICK: TOGGLE MINE FLAG ACTIONS
                if (e.Button == MouseButtons.Right)
                {
                    if (revealed[r, c]) return;

                    if (!flagged[r, c])
                    {
                        flagged[r, c] = true;
                        flagsPlaced++;
                        clickedButton.Text = "🚩";
                        clickedButton.ForeColor = Color.Red;
                    }
                    else
                    {
                        flagged[r, c] = false;
                        flagsPlaced--;
                        // If the first move hasn't happened yet, restore the helper 'X' indicator text
                        clickedButton.Text = (!firstClickTracked && r == designatedStartTile.X && c == designatedStartTile.Y) ? "X" : "";
                        clickedButton.ForeColor = Color.Red;
                    }
                    UpdateMineCounter();
                    CheckWinCondition(); // Re-evaluate fields for possible victory state changes
                }
                // LEFT-CLICK: ATTEMPT CELL REVEAL
                else if (e.Button == MouseButtons.Left)
                {
                    if (flagged[r, c] || revealed[r, c]) return; // Do nothing if flagged or already open

                    // Defeat condition handler
                    if (backendBoard[r, c] == -1)
                    {
                        TriggerGameOver();
                    }
                    else
                    {
                        RevealCell(r, c);   // Open selected tile location map data coordinates
                        CheckWinCondition(); // Check for win state conditions
                    }
                }
            }
        }

        /// Chord Action Tool: Clears neighboring empty spaces automatically if local flag counts match cell number requirements
        private void TriggerChordSweep(int r, int c)
        {
            int targetFlags = backendBoard[r, c];
            if (targetFlags <= 0) return;

            int actualFlagsCount = 0;
            // Count surrounding flag assets present inside the current neighborhood perimeter
            for (int i = -1; i <= 1; i++)
            {
                for (int j = -1; j <= 1; j++)
                {
                    if (IsValid(r + i, c + j) && flagged[r + i, c + j])
                        actualFlagsCount++;
                }
            }

            // Execute automated sweeps if requirements match
            if (actualFlagsCount == targetFlags)
            {
                for (int i = -1; i <= 1; i++)
                {
                    for (int j = -1; j <= 1; j++)
                    {
                        int nr = r + i;
                        int nc = c + j;
                        if (IsValid(nr, nc) && !revealed[nr, nc] && !flagged[nr, nc])
                        {
                            // If user placed an incorrect flag, chord actions will set off hidden mines immediately
                            if (backendBoard[nr, nc] == -1)
                            {
                                TriggerGameOver();
                                return;
                            }
                            else
                            {
                                RevealCell(nr, nc); // Open safe cells recursively
                            }
                        }
                    }
                }
                CheckWinCondition();
            }
        }

        /// Core Reveal Routine: Recursively opens empty spaces and formats number display graphics
        private void RevealCell(int r, int c)
        {
            if (!IsValid(r, c) || revealed[r, c] || flagged[r, c]) return;

            revealed[r, c] = true;
            Button btn = gridButtons[r, c];

            // Re-style opened tile from raised bevel button layout look into a flat inset style look
            btn.BackColor = Color.LightGray;
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize = 1;
            btn.FlatAppearance.BorderColor = Color.DarkGray;

            // Apply standard theme colors based on number values
            if (backendBoard[r, c] > 0)
            {
                btn.Text = backendBoard[r, c].ToString();
                if (backendBoard[r, c] == 1) btn.ForeColor = Color.Blue;
                else if (backendBoard[r, c] == 2) btn.ForeColor = Color.Green;
                else if (backendBoard[r, c] == 3) btn.ForeColor = Color.Red;
                else btn.ForeColor = Color.Purple; // 4+ numbers default into distinctive dark purple
            }
            else
            {
                btn.Text = ""; // Leave zero count slots perfectly empty for clean presentation look
            }

            // Zero Cascade Rule: If an opened spot is a 0, automatically open all neighboring coordinates
            if (backendBoard[r, c] == 0)
            {
                for (int i = -1; i <= 1; i++)
                {
                    for (int j = -1; j <= 1; j++)
                    {
                        RevealCell(r + i, c + j);
                    }
                }
            }
        }

        /// Defeat Manager Routine: Halts system clocks and highlights all mine locations in red
        private void TriggerGameOver()
        {
            gameTimer.Stop();
            RevealAllMines(); // Expose entire board layout architecture elements
            btnReset.Text = "😵";
            MessageBox.Show("💥 BOOM! Game Over!", "Minesweeper");
            gameOver = true;  // Lock all user input events across active components
        }

        /// Exposes every bomb item on the map grid upon a game over trigger
        private void RevealAllMines()
        {
            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    if (backendBoard[r, c] == -1)
                    {
                        gridButtons[r, c].Text = "💣";
                        gridButtons[r, c].BackColor = Color.Red; // Visually flags explosive location zones
                    }
                }
            }
        }

        /// Win-State Checker Routine: Assures absolute strict verification matching Minesweeper.online conditions
        private void CheckWinCondition()
        {
            if (!firstClickTracked) return;

            bool allSafeCleared = true;
            bool allMinesFlagged = true;

            // Scan the entire board state configuration matching against data parameters
            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    if (backendBoard[r, c] == -1)
                    {
                        // Condition 1: All actual bombs MUST be covered with a red flag item asset
                        if (!flagged[r, c])
                        {
                            allMinesFlagged = false;
                        }
                    }
                    else
                    {
                        // Condition 2: Every single safe number location MUST be open/revealed
                        if (!revealed[r, c])
                        {
                            allSafeCleared = false;
                        }
                        // Condition 3: Safe spaces MUST NOT contain an erroneous flag placement item
                        if (flagged[r, c])
                        {
                            allMinesFlagged = false;
                        }
                    }
                }
            }

            // Trigger victory sequence only when all criteria are met simultaneously
            if (allSafeCleared && allMinesFlagged)
            {
                gameTimer.Stop();
                btnReset.Text = "😎";
                MessageBox.Show("🎉 Perfect Run! Every tile cleared and every mine flagged!", "Winner");
                gameOver = true; // Lock down grid interactions
            }
        }
    }
}