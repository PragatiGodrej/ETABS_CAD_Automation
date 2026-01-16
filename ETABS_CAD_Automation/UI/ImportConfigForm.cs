// ============================================================================
// FILE: UI/ImportConfigForm.cs
// ============================================================================
using System;
using System.Collections.Generic;
using System.Windows.Forms;
using ETABS_CAD_Automation.Importers;
using ETABS_CAD_Automation.Models;

namespace ETABS_CAD_Automation
{
    public partial class ImportConfigForm : Form
    {
        public List<FloorTypeConfig> FloorConfigs { get; private set; }
        public string SeismicZone { get; private set; }

        // UI Controls
        private TabControl tabControl;
        private Button btnImport;
        private Button btnCancel;
        private CheckBox chkBasement;
        private CheckBox chkPodium;
        private NumericUpDown numBasementLevels;
        private NumericUpDown numPodiumLevels;
        private NumericUpDown numTypicalLevels;
        private NumericUpDown numBasementHeight;
        private NumericUpDown numPodiumHeight;
        private NumericUpDown numEDeckHeight;
        private NumericUpDown numTypicalHeight;
        private ComboBox cmbSeismicZone;

        // CAD Import controls (dynamic per floor type)
        private Dictionary<string, TextBox> cadPathTextBoxes;
        private Dictionary<string, ListBox> availableLayerListBoxes;
        private Dictionary<string, ListBox> mappedLayerListBoxes;
        private Dictionary<string, ComboBox> elementTypeComboBoxes;

        public ImportConfigForm()
        {
            InitializeComponent();
            FloorConfigs = new List<FloorTypeConfig>();
            cadPathTextBoxes = new Dictionary<string, TextBox>();
            availableLayerListBoxes = new Dictionary<string, ListBox>();
            mappedLayerListBoxes = new Dictionary<string, ListBox>();
            elementTypeComboBoxes = new Dictionary<string, ComboBox>();
            InitializeControls();
        }

        private void InitializeControls()
        {
            this.Size = new System.Drawing.Size(900, 750);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Text = "Import CAD & Configure Building - Multi-Floor Types";

            tabControl = new TabControl
            {
                Location = new System.Drawing.Point(10, 10),
                Size = new System.Drawing.Size(870, 630)
            };
            this.Controls.Add(tabControl);

            // Tab 1: Building Configuration
            TabPage tabBuilding = new TabPage("Building Configuration");
            tabControl.TabPages.Add(tabBuilding);
            InitializeBuildingConfigTab(tabBuilding);

            // Action Buttons
            btnImport = new Button
            {
                Text = "Import to ETABS",
                Location = new System.Drawing.Point(600, 660),
                Size = new System.Drawing.Size(140, 40),
                Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold)
            };
            btnImport.Click += BtnImport_Click;
            this.Controls.Add(btnImport);

            btnCancel = new Button
            {
                Text = "Cancel",
                Location = new System.Drawing.Point(750, 660),
                Size = new System.Drawing.Size(130, 40),
                DialogResult = DialogResult.Cancel
            };
            this.Controls.Add(btnCancel);
            this.CancelButton = btnCancel;
        }

        private void InitializeBuildingConfigTab(TabPage tab)
        {
            tab.AutoScroll = true;
            int yPos = 20;

            // Instructions
            Label lblInstructions = new Label
            {
                Text = "📋 Define building structure: Basement → Podium → E-Deck (Ground) → Typical Floors",
                Location = new System.Drawing.Point(20, yPos),
                Size = new System.Drawing.Size(800, 25),
                Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold | System.Drawing.FontStyle.Italic),
                ForeColor = System.Drawing.Color.DarkBlue
            };
            tab.Controls.Add(lblInstructions);
            yPos += 35;

            // BASEMENT SECTION
            GroupBox grpBasement = new GroupBox
            {
                Text = "Basement Floors",
                Location = new System.Drawing.Point(20, yPos),
                Size = new System.Drawing.Size(820, 90)
            };
            tab.Controls.Add(grpBasement);

            chkBasement = new CheckBox
            {
                Text = "Include Basement Floors",
                Location = new System.Drawing.Point(20, 25),
                Size = new System.Drawing.Size(200, 20)
            };
            chkBasement.CheckedChanged += ChkBasement_CheckedChanged;
            grpBasement.Controls.Add(chkBasement);

            Label lblBasementCount = new Label
            {
                Text = "Number of Basements:",
                Location = new System.Drawing.Point(40, 52),
                Size = new System.Drawing.Size(150, 20)
            };
            grpBasement.Controls.Add(lblBasementCount);

            numBasementLevels = new NumericUpDown
            {
                Location = new System.Drawing.Point(200, 50),
                Size = new System.Drawing.Size(80, 25),
                Minimum = 1,
                Maximum = 10,
                Value = 2,
                Enabled = false
            };
            grpBasement.Controls.Add(numBasementLevels);

            Label lblBasementHeight = new Label
            {
                Text = "Basement Height (m):",
                Location = new System.Drawing.Point(320, 52),
                Size = new System.Drawing.Size(150, 20)
            };
            grpBasement.Controls.Add(lblBasementHeight);

            numBasementHeight = new NumericUpDown
            {
                Location = new System.Drawing.Point(480, 50),
                Size = new System.Drawing.Size(80, 25),
                DecimalPlaces = 2,
                Minimum = 2.5M,
                Maximum = 6.0M,
                Value = 3.5M,
                Increment = 0.1M,
                Enabled = false
            };
            grpBasement.Controls.Add(numBasementHeight);

            yPos += 100;

            // PODIUM SECTION
            GroupBox grpPodium = new GroupBox
            {
                Text = "Podium Floors",
                Location = new System.Drawing.Point(20, yPos),
                Size = new System.Drawing.Size(820, 90)
            };
            tab.Controls.Add(grpPodium);

            chkPodium = new CheckBox
            {
                Text = "Include Podium Floors",
                Location = new System.Drawing.Point(20, 25),
                Size = new System.Drawing.Size(200, 20)
            };
            chkPodium.CheckedChanged += ChkPodium_CheckedChanged;
            grpPodium.Controls.Add(chkPodium);

            Label lblPodiumCount = new Label
            {
                Text = "Number of Podiums:",
                Location = new System.Drawing.Point(40, 52),
                Size = new System.Drawing.Size(150, 20)
            };
            grpPodium.Controls.Add(lblPodiumCount);

            numPodiumLevels = new NumericUpDown
            {
                Location = new System.Drawing.Point(200, 50),
                Size = new System.Drawing.Size(80, 25),
                Minimum = 1,
                Maximum = 5,
                Value = 1,
                Enabled = false
            };
            grpPodium.Controls.Add(numPodiumLevels);

            Label lblPodiumHeight = new Label
            {
                Text = "Podium Height (m):",
                Location = new System.Drawing.Point(320, 52),
                Size = new System.Drawing.Size(150, 20)
            };
            grpPodium.Controls.Add(lblPodiumHeight);

            numPodiumHeight = new NumericUpDown
            {
                Location = new System.Drawing.Point(480, 50),
                Size = new System.Drawing.Size(80, 25),
                DecimalPlaces = 2,
                Minimum = 3.0M,
                Maximum = 8.0M,
                Value = 4.5M,
                Increment = 0.1M,
                Enabled = false
            };
            grpPodium.Controls.Add(numPodiumHeight);

            yPos += 100;

            // E-DECK (GROUND) SECTION
            GroupBox grpEDeck = new GroupBox
            {
                Text = "E-Deck (Ground Floor)",
                Location = new System.Drawing.Point(20, yPos),
                Size = new System.Drawing.Size(820, 70)
            };
            tab.Controls.Add(grpEDeck);

            Label lblEDeckHeight = new Label
            {
                Text = "E-Deck Height (m):",
                Location = new System.Drawing.Point(40, 32),
                Size = new System.Drawing.Size(150, 20)
            };
            grpEDeck.Controls.Add(lblEDeckHeight);

            numEDeckHeight = new NumericUpDown
            {
                Location = new System.Drawing.Point(200, 30),
                Size = new System.Drawing.Size(80, 25),
                DecimalPlaces = 2,
                Minimum = 3.0M,
                Maximum = 10.0M,
                Value = 4.5M,
                Increment = 0.1M
            };
            grpEDeck.Controls.Add(numEDeckHeight);

            Label lblEDeckNote = new Label
            {
                Text = "(Ground floor is mandatory)",
                Location = new System.Drawing.Point(290, 32),
                Size = new System.Drawing.Size(200, 20),
                Font = new System.Drawing.Font("Segoe UI", 8F, System.Drawing.FontStyle.Italic),
                ForeColor = System.Drawing.Color.Gray
            };
            grpEDeck.Controls.Add(lblEDeckNote);

            yPos += 80;

            // TYPICAL FLOORS SECTION
            GroupBox grpTypical = new GroupBox
            {
                Text = "Typical Floors (Above E-Deck)",
                Location = new System.Drawing.Point(20, yPos),
                Size = new System.Drawing.Size(820, 90)
            };
            tab.Controls.Add(grpTypical);

            Label lblTypicalCount = new Label
            {
                Text = "Number of Typical Floors:",
                Location = new System.Drawing.Point(40, 32),
                Size = new System.Drawing.Size(160, 20)
            };
            grpTypical.Controls.Add(lblTypicalCount);

            numTypicalLevels = new NumericUpDown
            {
                Location = new System.Drawing.Point(210, 30),
                Size = new System.Drawing.Size(80, 25),
                Minimum = 1,
                Maximum = 100,
                Value = 10
            };
            grpTypical.Controls.Add(numTypicalLevels);

            Label lblTypicalHeight = new Label
            {
                Text = "Typical Floor Height (m):",
                Location = new System.Drawing.Point(320, 32),
                Size = new System.Drawing.Size(160, 20)
            };
            grpTypical.Controls.Add(lblTypicalHeight);

            numTypicalHeight = new NumericUpDown
            {
                Location = new System.Drawing.Point(490, 30),
                Size = new System.Drawing.Size(80, 25),
                DecimalPlaces = 2,
                Minimum = 2.8M,
                Maximum = 5.0M,
                Value = 3.0M,
                Increment = 0.1M
            };
            grpTypical.Controls.Add(numTypicalHeight);

            yPos += 100;

            // SEISMIC ZONE
            GroupBox grpSeismic = new GroupBox
            {
                Text = "Seismic Parameters",
                Location = new System.Drawing.Point(20, yPos),
                Size = new System.Drawing.Size(820, 70)
            };
            tab.Controls.Add(grpSeismic);

            Label lblSeismic = new Label
            {
                Text = "Seismic Zone:",
                Location = new System.Drawing.Point(40, 32),
                Size = new System.Drawing.Size(120, 20)
            };
            grpSeismic.Controls.Add(lblSeismic);

            cmbSeismicZone = new ComboBox
            {
                Location = new System.Drawing.Point(170, 30),
                Size = new System.Drawing.Size(150, 25),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cmbSeismicZone.Items.AddRange(new object[] { "Zone II", "Zone III", "Zone IV", "Zone V" });
            cmbSeismicZone.SelectedIndex = 2;
            grpSeismic.Controls.Add(cmbSeismicZone);

            yPos += 80;

            // GENERATE CAD IMPORT TABS BUTTON
            Button btnGenerateTabs = new Button
            {
                Text = "Generate CAD Import Tabs →",
                Location = new System.Drawing.Point(320, yPos),
                Size = new System.Drawing.Size(200, 40),
                Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold),
                BackColor = System.Drawing.Color.LightGreen
            };
            btnGenerateTabs.Click += BtnGenerateTabs_Click;
            tab.Controls.Add(btnGenerateTabs);
        }

        private void ChkBasement_CheckedChanged(object sender, EventArgs e)
        {
            numBasementLevels.Enabled = chkBasement.Checked;
            numBasementHeight.Enabled = chkBasement.Checked;
        }

        private void ChkPodium_CheckedChanged(object sender, EventArgs e)
        {
            numPodiumLevels.Enabled = chkPodium.Checked;
            numPodiumHeight.Enabled = chkPodium.Checked;
        }

        private void BtnGenerateTabs_Click(object sender, EventArgs e)
        {
            // Remove old CAD import tabs
            while (tabControl.TabPages.Count > 1)
            {
                tabControl.TabPages.RemoveAt(1);
            }

            // Clear dictionaries
            cadPathTextBoxes.Clear();
            availableLayerListBoxes.Clear();
            mappedLayerListBoxes.Clear();
            elementTypeComboBoxes.Clear();

            // Generate tabs based on configuration
            if (chkBasement.Checked)
            {
                CreateCADImportTab("Basement", "Basement Floor Plan");
            }

            if (chkPodium.Checked)
            {
                CreateCADImportTab("Podium", "Podium Floor Plan");
            }

            CreateCADImportTab("EDeck", "E-Deck (Ground) Floor Plan");
            CreateCADImportTab("Typical", "Typical Floor Plan (Will be replicated)");

            MessageBox.Show(
                "CAD Import tabs generated!\n\n" +
                "Please upload CAD files and map layers for each floor type.",
                "Tabs Generated",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        private void CreateCADImportTab(string floorType, string description)
        {
            TabPage tab = new TabPage($"{floorType} - CAD Import");
            tabControl.TabPages.Add(tab);

            // Description
            Label lblDesc = new Label
            {
                Text = $"📐 {description}",
                Location = new System.Drawing.Point(20, 10),
                Size = new System.Drawing.Size(800, 25),
                Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold),
                ForeColor = System.Drawing.Color.DarkGreen
            };
            tab.Controls.Add(lblDesc);

            // CAD File Selection
            Label lblCAD = new Label
            {
                Text = "CAD File:",
                Location = new System.Drawing.Point(20, 45),
                Size = new System.Drawing.Size(100, 20)
            };
            tab.Controls.Add(lblCAD);

            TextBox txtCADPath = new TextBox
            {
                Location = new System.Drawing.Point(120, 43),
                Size = new System.Drawing.Size(540, 25),
                ReadOnly = true
            };
            tab.Controls.Add(txtCADPath);
            cadPathTextBoxes[floorType] = txtCADPath;

            Button btnLoadCAD = new Button
            {
                Text = "Browse...",
                Location = new System.Drawing.Point(670, 41),
                Size = new System.Drawing.Size(120, 28)
            };
            btnLoadCAD.Click += (s, ev) => BtnLoadCAD_Click(floorType);
            tab.Controls.Add(btnLoadCAD);

            // Available Layers
            Label lblAvailable = new Label
            {
                Text = "Available CAD Layers:",
                Location = new System.Drawing.Point(20, 85),
                Size = new System.Drawing.Size(200, 20)
            };
            tab.Controls.Add(lblAvailable);

            ListBox lstAvailable = new ListBox
            {
                Location = new System.Drawing.Point(20, 110),
                Size = new System.Drawing.Size(280, 400)
            };
            tab.Controls.Add(lstAvailable);
            availableLayerListBoxes[floorType] = lstAvailable;

            // Element Type Selection
            Label lblType = new Label
            {
                Text = "Assign as:",
                Location = new System.Drawing.Point(320, 110),
                Size = new System.Drawing.Size(100, 20)
            };
            tab.Controls.Add(lblType);

            ComboBox cboElement = new ComboBox
            {
                Location = new System.Drawing.Point(320, 135),
                Size = new System.Drawing.Size(140, 25),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cboElement.Items.AddRange(new object[] { "Beam", "Wall", "Slab", "Ignore" });
            cboElement.SelectedIndex = 0;
            tab.Controls.Add(cboElement);
            elementTypeComboBoxes[floorType] = cboElement;

            Button btnAdd = new Button
            {
                Text = "Add →",
                Location = new System.Drawing.Point(320, 170),
                Size = new System.Drawing.Size(140, 35)
            };
            btnAdd.Click += (s, ev) => BtnAddMapping_Click(floorType);
            tab.Controls.Add(btnAdd);

            Button btnRemove = new Button
            {
                Text = "← Remove",
                Location = new System.Drawing.Point(320, 215),
                Size = new System.Drawing.Size(140, 35)
            };
            btnRemove.Click += (s, ev) => BtnRemoveMapping_Click(floorType);
            tab.Controls.Add(btnRemove);

            // Mapped Layers
            Label lblMapped = new Label
            {
                Text = "Layer Mappings:",
                Location = new System.Drawing.Point(480, 85),
                Size = new System.Drawing.Size(200, 20)
            };
            tab.Controls.Add(lblMapped);

            ListBox lstMapped = new ListBox
            {
                Location = new System.Drawing.Point(480, 110),
                Size = new System.Drawing.Size(310, 400)
            };
            tab.Controls.Add(lstMapped);
            mappedLayerListBoxes[floorType] = lstMapped;
        }

        private void BtnLoadCAD_Click(string floorType)
        {
            OpenFileDialog ofd = new OpenFileDialog
            {
                Filter = "AutoCAD Files (*.dwg;*.dxf)|*.dwg;*.dxf",
                Title = $"Select CAD File for {floorType}"
            };

            if (ofd.ShowDialog() != DialogResult.OK) return;

            cadPathTextBoxes[floorType].Text = ofd.FileName;

            CADLayerReader reader = new CADLayerReader();
            List<string> layers = reader.GetLayerNamesFromFile(ofd.FileName);

            availableLayerListBoxes[floorType].Items.Clear();
            foreach (string layer in layers)
            {
                availableLayerListBoxes[floorType].Items.Add(layer);
            }

            AutoMapLayers(floorType, layers);
        }

        private void AutoMapLayers(string floorType, List<string> layers)
        {
            mappedLayerListBoxes[floorType].Items.Clear();

            foreach (string layer in layers)
            {
                string elementType = null;

                if (layer.StartsWith("B-") || layer.Contains("Beam") || layer.Contains("BEAM"))
                    elementType = "Beam";
                else if (layer.Contains("wall") || layer.Contains("Wall") || layer.Contains("WALL"))
                    elementType = "Wall";
                else if (layer.StartsWith("S-") || layer.Contains("Slab") || layer.Contains("SLAB"))
                    elementType = "Slab";

                if (elementType != null)
                {
                    mappedLayerListBoxes[floorType].Items.Add($"{layer} → {elementType}");
                }
            }
        }

        private void BtnAddMapping_Click(string floorType)
        {
            if (availableLayerListBoxes[floorType].SelectedItem == null)
            {
                MessageBox.Show("Please select a layer to map.", "Info");
                return;
            }

            string layerName = availableLayerListBoxes[floorType].SelectedItem.ToString();
            string elementType = elementTypeComboBoxes[floorType].SelectedItem.ToString();

            if (elementType == "Ignore") return;

            string mapping = $"{layerName} → {elementType}";
            if (!mappedLayerListBoxes[floorType].Items.Contains(mapping))
            {
                mappedLayerListBoxes[floorType].Items.Add(mapping);
            }
            else
            {
                MessageBox.Show("Layer already mapped.", "Info");
            }
        }

        private void BtnRemoveMapping_Click(string floorType)
        {
            if (mappedLayerListBoxes[floorType].SelectedItem == null)
            {
                MessageBox.Show("Please select a mapping to remove.", "Info");
                return;
            }

            mappedLayerListBoxes[floorType].Items.Remove(
                mappedLayerListBoxes[floorType].SelectedItem);
        }

        private void BtnImport_Click(object sender, EventArgs e)
        {
            // Validate and collect all configurations
            FloorConfigs.Clear();

            // Basement
            if (chkBasement.Checked)
            {
                if (!ValidateFloorConfig("Basement"))
                {
                    MessageBox.Show("Please configure Basement CAD file and layer mappings.",
                        "Validation Error");
                    return;
                }

                FloorConfigs.Add(new FloorTypeConfig
                {
                    Name = "Basement",
                    Count = (int)numBasementLevels.Value,
                    Height = (double)numBasementHeight.Value,
                    CADFilePath = cadPathTextBoxes["Basement"].Text,
                    LayerMapping = GetLayerMapping("Basement")
                });
            }

            // Podium
            if (chkPodium.Checked)
            {
                if (!ValidateFloorConfig("Podium"))
                {
                    MessageBox.Show("Please configure Podium CAD file and layer mappings.",
                        "Validation Error");
                    return;
                }

                FloorConfigs.Add(new FloorTypeConfig
                {
                    Name = "Podium",
                    Count = (int)numPodiumLevels.Value,
                    Height = (double)numPodiumHeight.Value,
                    CADFilePath = cadPathTextBoxes["Podium"].Text,
                    LayerMapping = GetLayerMapping("Podium")
                });
            }

            // E-Deck (Ground)
            if (!ValidateFloorConfig("EDeck"))
            {
                MessageBox.Show("Please configure E-Deck (Ground) CAD file and layer mappings.",
                    "Validation Error");
                return;
            }

            FloorConfigs.Add(new FloorTypeConfig
            {
                Name = "EDeck",
                Count = 1,
                Height = (double)numEDeckHeight.Value,
                CADFilePath = cadPathTextBoxes["EDeck"].Text,
                LayerMapping = GetLayerMapping("EDeck")
            });

            // Typical
            if (!ValidateFloorConfig("Typical"))
            {
                MessageBox.Show("Please configure Typical floor CAD file and layer mappings.",
                    "Validation Error");
                return;
            }

            FloorConfigs.Add(new FloorTypeConfig
            {
                Name = "Typical",
                Count = (int)numTypicalLevels.Value,
                Height = (double)numTypicalHeight.Value,
                CADFilePath = cadPathTextBoxes["Typical"].Text,
                LayerMapping = GetLayerMapping("Typical")
            });

            SeismicZone = cmbSeismicZone.SelectedItem?.ToString() ?? "Zone IV";

            // Show confirmation
            ShowConfirmation();
        }

        private bool ValidateFloorConfig(string floorType)
        {
            return cadPathTextBoxes.ContainsKey(floorType) &&
                   !string.IsNullOrEmpty(cadPathTextBoxes[floorType].Text) &&
                   mappedLayerListBoxes.ContainsKey(floorType) &&
                   mappedLayerListBoxes[floorType].Items.Count > 0;
        }

        private Dictionary<string, string> GetLayerMapping(string floorType)
        {
            Dictionary<string, string> mapping = new Dictionary<string, string>();

            if (mappedLayerListBoxes.ContainsKey(floorType))
            {
                foreach (var item in mappedLayerListBoxes[floorType].Items)
                {
                    string[] parts = item.ToString().Split(new[] { " → " }, StringSplitOptions.None);
                    if (parts.Length == 2)
                    {
                        mapping[parts[0]] = parts[1];
                    }
                }
            }

            return mapping;
        }

        private void ShowConfirmation()
        {
            int totalStories = 0;
            double totalHeight = 0;
            string breakdown = "═══════════════════════════════════════\n";
            breakdown += "  IMPORT CONFIGURATION SUMMARY\n";
            breakdown += "═══════════════════════════════════════\n\n";

            breakdown += "🏢 BUILDING STRUCTURE:\n\n";

            double cumulativeHeight = 0;
            foreach (var config in FloorConfigs)
            {
                totalStories += config.Count;
                double sectionHeight = config.Height * config.Count;
                totalHeight += sectionHeight;

                breakdown += $"  {config.Name}:\n";
                breakdown += $"    • Floors: {config.Count}\n";
                breakdown += $"    • Height per floor: {config.Height:F2}m\n";
                breakdown += $"    • Section height: {sectionHeight:F2}m\n";
                breakdown += $"    • Elevation range: {cumulativeHeight:F2}m - {cumulativeHeight + sectionHeight:F2}m\n";
                breakdown += $"    • CAD: {System.IO.Path.GetFileName(config.CADFilePath)}\n";
                breakdown += $"    • Layers mapped: {config.LayerMapping.Count}\n\n";

                cumulativeHeight += sectionHeight;
            }

            breakdown += "📊 TOTALS:\n";
            breakdown += $"  • Total Stories: {totalStories}\n";
            breakdown += $"  • Total Building Height: {totalHeight:F2}m\n";
            breakdown += $"  • Seismic Zone: {SeismicZone}\n\n";
            breakdown += "═══════════════════════════════════════\n";
            breakdown += "Proceed with import?";

            var result = MessageBox.Show(breakdown, "⚠️ Confirm Import",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
        }
    }
}