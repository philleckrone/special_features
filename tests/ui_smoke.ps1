<#
.SYNOPSIS
  UI Automation smoke test of the Special Features window, run through the test host on a copy
  of a project folder: takes screenshots, checks the summary, presses "Go to in Paratext" (which
  must ask for the first row's verse), unticks every publication but one and ticks "Hide places
  that already have special formatting" (the list must shrink), adds a place, hides the first
  place, shows hidden places and shows it again, leaves out "Minor divisions" in the Kinds
  chooser, then closes the window and opens it again to check that the choices were kept.

  The mouse is never moved and nothing is typed: button presses and ticks are posted as window
  messages to the test window only.

.EXAMPLE
  .\ui_smoke.ps1 -Project ..\..\TestProject\PDT15 -Out C:\temp\sf -Lang es
  -Lang en, es or fr: the window's language, which the control names below follow.
#>
param(
    [Parameter(Mandatory = $true)][string]$Project,
    [Parameter(Mandatory = $true)][string]$Out,
    [string]$Lang = 'en',
    [string]$ExePath = ''
)
$ErrorActionPreference = 'Stop'
if (-not $ExePath) { $ExePath = Join-Path $PSScriptRoot 'TestHost\bin\Release\SpecialFeaturesTestHost.exe' }
Add-Type -AssemblyName UIAutomationClient, UIAutomationTypes, System.Drawing
Add-Type @'
using System;
using System.Runtime.InteropServices;
public static class SfWin {
    [StructLayout(LayoutKind.Sequential)] public struct Rect { public int L, T, R, B; }
    [DllImport("user32.dll")] public static extern bool SetProcessDPIAware();
    [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr h, out Rect r);
    [DllImport("user32.dll")] public static extern bool PrintWindow(IntPtr h, IntPtr hdc, int flags);
    [DllImport("user32.dll")] public static extern bool PostMessage(IntPtr h, int msg, IntPtr w, IntPtr l);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] public static extern IntPtr SendMessage(IntPtr h, int msg, IntPtr w, string l);
    [DllImport("user32.dll")] public static extern IntPtr SendMessage(IntPtr h, int msg, IntPtr w, IntPtr l);
    public static void PressButton(IntPtr h) { PostMessage(h, 0xF5, IntPtr.Zero, IntPtr.Zero); }
    /// <summary>WM_SETTEXT: a WinForms TextBox reads its text from the window.</summary>
    public static void SetText(IntPtr h, string text) { SendMessage(h, 0x000C, IntPtr.Zero, text); }
    /// <summary>CB_SETCURSEL: a WinForms ComboBox reads SelectedIndex from the window.</summary>
    public static void SelectIndex(IntPtr h, int i) { SendMessage(h, 0x014E, (IntPtr)i, IntPtr.Zero); }
    public static void PostKey(IntPtr h, int vk) {
        PostMessage(h, 0x0100, (IntPtr)vk, (IntPtr)1);
        PostMessage(h, 0x0101, (IntPtr)vk, (IntPtr)unchecked((int)0xC0000001));
    }
    [DllImport("user32.dll")] static extern IntPtr SendMessage(IntPtr h, int msg, IntPtr w, ref Rect r);
    /// <summary>A click posted on a list-box item (a CheckedListBox with CheckOnClick ticks it).</summary>
    public static void ListClickItem(IntPtr h, int i) {
        Rect r = new Rect();
        SendMessage(h, 0x198, (IntPtr)i, ref r);   // LB_GETITEMRECT
        IntPtr lp = (IntPtr)((((r.T + r.B) / 2) << 16) | ((r.L + 8) & 0xFFFF));
        PostMessage(h, 0x0201, (IntPtr)1, lp);
        PostMessage(h, 0x0202, IntPtr.Zero, lp);
    }
    public static int ListCount(IntPtr h) { return (int)SendMessage(h, 0x18B, IntPtr.Zero, IntPtr.Zero); }
}
'@
[SfWin]::SetProcessDPIAware() | Out-Null
# The control names in each language (accented letters as [char], so this file stays ASCII).
$e = [char]0xE9; $a = [char]0xE0; $n = [char]0xF1
$names = @{
    en = @{ GoTo = 'Go to in Paratext'; Quotations = 'OT quotations'; Hide = 'Hide places that already have special formatting';
            AddedButton = 'Added places...'; AddedTitle = 'Added places'; Add = 'Add...'; Place = 'Place'; Ok = 'OK'; Close = 'Close';
            HideSel = 'Hide selected'; Unhide = 'Show selected again'; ShowHidden = '^Show hidden places \(\d+\)$';
            Kinds = '^Kinds: \d+ of \d+\.\.\.$'; KindsTitle = 'Kinds of places'; Hidden = 'hidden' }
    es = @{ GoTo = 'Ir en Paratext'; Quotations = 'Citas del AT'; Hide = 'Ocultar los lugares que ya tienen formato especial';
            AddedButton = "Lugares a${n}adidos..."; AddedTitle = "Lugares a${n}adidos"; Add = "A${n}adir..."; Place = 'Lugar'; Ok = 'Aceptar'; Close = 'Cerrar';
            HideSel = 'Ocultar los seleccionados'; Unhide = 'Volver a mostrar los seleccionados'; ShowHidden = '^Mostrar los lugares ocultos \(\d+\)$';
            Kinds = '^Tipos: \d+ de \d+\.\.\.$'; KindsTitle = 'Tipos de lugares'; Hidden = 'ocultos' }
    fr = @{ GoTo = 'Aller dans Paratext'; Quotations = "Citations de l'AT"; Hide = "Masquer les endroits qui ont d${e}j${a} une mise en forme sp${e}ciale";
            AddedButton = "Endroits ajout${e}s..."; AddedTitle = "Endroits ajout${e}s"; Add = 'Ajouter...'; Place = 'Endroit'; Ok = 'OK'; Close = 'Fermer';
            HideSel = "Masquer la s${e}lection"; Unhide = "R${e}afficher la s${e}lection"; ShowHidden = "^Afficher les endroits masqu${e}s \(\d+\)$";
            Kinds = '^Types : \d+ sur \d+\.\.\.$'; KindsTitle = "Types d'endroits"; Hidden = "masqu${e}s" }
}
$L = $names[$Lang.Substring(0, [Math]::Min(2, $Lang.Length)).ToLowerInvariant()]
if (-not $L) { $L = $names['en'] }

New-Item -ItemType Directory -Force $Out | Out-Null
$copy = Join-Path $Out 'project'
if (Test-Path $copy) { Remove-Item -Recurse -Force $copy }
New-Item -ItemType Directory -Force $copy | Out-Null
# Only the book files (named like 41MATPDT15.SFM) and Settings.xml: a stray RegExBackup1.sfm also starts with \id GEN.
Get-ChildItem -LiteralPath $Project -File | Where-Object { $_.Extension -notin '.bak', '.html' } | Copy-Item -Destination $copy

$gotoLog = Join-Path $Out 'goto.txt'
if (Test-Path $gotoLog) { Remove-Item $gotoLog }
# The added places go to a file in the output folder, never to the real list in %APPDATA%.
$addedFile = Join-Path $Out 'added.tsv'
if (Test-Path $addedFile) { Remove-Item $addedFile }

$AE = [Windows.Automation.AutomationElement]
$Scope = [Windows.Automation.TreeScope]
$CT = [Windows.Automation.ControlType]
$root = $AE::RootElement

function StartWindow {
    $psi = New-Object Diagnostics.ProcessStartInfo $ExePath
    $psi.Arguments = "form `"$copy`" $Lang"
    $psi.EnvironmentVariables['SF_GOTOLOG'] = $gotoLog
    $psi.EnvironmentVariables['SF_ADDED'] = $addedFile
    $psi.UseShellExecute = $false
    $psi.RedirectStandardOutput = $true
    $script:proc = [Diagnostics.Process]::Start($psi)
    $script:byProcess = New-Object Windows.Automation.PropertyCondition ($AE::ProcessIdProperty, $script:proc.Id)
    $script:win = $null
    for ($i = 0; $i -lt 100 -and -not $script:win; $i++) {
        Start-Sleep -Milliseconds 200
        $script:win = $root.FindFirst($Scope::Children, $script:byProcess)
    }
    if (-not $script:win) { throw 'window did not appear' }
}
StartWindow

function Shot($name) {
    $h = [IntPtr]$win.Current.NativeWindowHandle
    $r = New-Object SfWin+Rect
    [SfWin]::GetWindowRect($h, [ref]$r) | Out-Null
    $bmp = New-Object Drawing.Bitmap ($r.R - $r.L), ($r.B - $r.T)
    $g = [Drawing.Graphics]::FromImage($bmp)
    $hdc = $g.GetHdc()
    [SfWin]::PrintWindow($h, $hdc, 2) | Out-Null
    $g.ReleaseHdc($hdc)
    $bmp.Save((Join-Path $Out $name), [Drawing.Imaging.ImageFormat]::Png)
    $g.Dispose(); $bmp.Dispose()
    "SHOT $name"
}

function Summary {
    @($win.FindAll($Scope::Descendants, (New-Object Windows.Automation.PropertyCondition ($AE::ControlTypeProperty, $CT::Text)))) |
        ForEach-Object { $_.Current.Name } | Where-Object { $_ -match '^\d+ (places shown|lugares mostrados|endroits affich)' } | Select-Object -First 1
}

function WaitSummary($notEqual) {
    for ($i = 0; $i -lt 150; $i++) {
        $s = Summary
        if ($s -and $s -ne $notEqual) { return $s }
        Start-Sleep -Milliseconds 200
    }
    throw "summary did not appear or change (was: $notEqual)"
}

# WinForms buttons and tick boxes reach UI Automation as plain panes, so they are found by
# name alone and pressed with a posted BM_CLICK (which also toggles a tick box).
function Press($name) {
    $c = New-Object Windows.Automation.PropertyCondition ($AE::NameProperty, $name)
    $el = $win.FindFirst($Scope::Descendants, $c)
    if (-not $el -or -not $el.Current.NativeWindowHandle) { throw "control '$name' not found" }
    [SfWin]::PressButton([IntPtr]$el.Current.NativeWindowHandle)
}

# A control whose name matches a pattern (the Kinds button and the Show hidden tick box carry counts).
function Matching($pattern) {
    $el = @($win.FindAll($Scope::Descendants, [Windows.Automation.Condition]::TrueCondition)) |
        Where-Object { $_.Current.Name -match $pattern } | Select-Object -First 1
    if (-not $el) { throw "control matching '$pattern' not found" }
    return $el
}

function ShownCount($summary) { return [int]($summary -replace '^(\d+).*', '$1') }

# Page Up on the grid makes the first row the current (and only selected) one.
function FirstRow {
    $grid = @($win.FindAll($Scope::Descendants, [Windows.Automation.Condition]::TrueCondition)) |
        Where-Object { $_.Current.ControlType -eq $CT::Table } | Select-Object -First 1
    if (-not $grid) { throw 'grid not found' }
    for ($k = 0; $k -lt 6; $k++) { [SfWin]::PostKey([IntPtr]$grid.Current.NativeWindowHandle, 0x21) }
    Start-Sleep -Milliseconds 600
}

$script:fails = 0
function Check($what, $ok, $detail) {
    if ($ok) { "PASS $what" } else { "FAIL $what ($detail)"; $script:fails++ }
}

$all = WaitSummary $null
"SUMMARY $all"
Shot 'all.png'

# The first row is selected when the list is filled: "Go to" asks for its first verse.
Press $L.GoTo
for ($i = 0; $i -lt 25 -and -not (Test-Path $gotoLog); $i++) { Start-Sleep -Milliseconds 200 }
if (-not (Test-Path $gotoLog)) { throw 'go to was not requested' }
"GOTO $(Get-Content $gotoLog -TotalCount 1)"

# Only places NIV formats specially, then hide those already formatted.
foreach ($p in 'BFC', 'CEV', 'GNB', 'NVI', 'NLT', 'TEV', $L.Quotations) { Press $p; Start-Sleep -Milliseconds 300 }
$niv = WaitSummary $all
"SUMMARY NIV $niv"
Press $L.Hide
$plain = WaitSummary $niv
"SUMMARY NIV PLAIN $plain"
Shot 'niv_plain.png'

$settings = Join-Path $copy 'local\SpecialFeatures\settings.xml'
if (-not (Test-Path $settings)) { throw 'settings were not saved' }
"SETTINGS $((Get-Content $settings -Raw) -replace '\s+', ' ')"

# Added places: add NEH 3 (a list, source "Peru") through the two dialogs. It is plain in PDT15,
# and the new source "Peru" is ticked, so the list must grow by one.
function TopWindow($name) {
    for ($i = 0; $i -lt 50; $i++) {
        # A dialog can be a top-level window or sit under the window that opened it.
        $c1 = New-Object Windows.Automation.PropertyCondition ($AE::NameProperty, $name)
        $c2 = New-Object Windows.Automation.PropertyCondition ($AE::ControlTypeProperty, $CT::Window)
        $named = New-Object Windows.Automation.AndCondition $c1, $c2
        foreach ($top in @($root.FindAll($Scope::Children, $byProcess))) {
            if ($top.Current.Name -eq $name) { return $top }
            $w = $top.FindFirst($Scope::Descendants, $named)
            if ($w) { return $w }
        }
        Start-Sleep -Milliseconds 200
    }
    throw "window '$name' did not appear"
}
function PressIn($w, $name) {
    $el = $w.FindFirst($Scope::Descendants, (New-Object Windows.Automation.PropertyCondition ($AE::NameProperty, $name)))
    if (-not $el) { throw "control '$name' not found" }
    [SfWin]::PressButton([IntPtr]$el.Current.NativeWindowHandle)
}
Press $L.AddedButton
$manager = TopWindow $L.AddedTitle
PressIn $manager $L.Add
$edit = TopWindow $L.Place
# WinForms text boxes reach UI Automation as panes: set them with WM_SETTEXT, in layout order
# (references, description, source); the first combo box is the book (NEH is book 16).
$all = @($edit.FindAll($Scope::Descendants, [Windows.Automation.Condition]::TrueCondition))
$boxes = @($all | Where-Object { $_.Current.ClassName -match '\.EDIT\.' } | Sort-Object { $_.Current.BoundingRectangle.Y })
$combos = @($all | Where-Object { $_.Current.ClassName -match '\.COMBOBOX\.' } | Sort-Object { $_.Current.BoundingRectangle.Y })
if ($boxes.Count -lt 3 -or $combos.Count -lt 1) { throw "expected 3 text boxes and a combo box, found $($boxes.Count) and $($combos.Count)" }
[SfWin]::SelectIndex([IntPtr]$combos[0].Current.NativeWindowHandle, 15)
[SfWin]::SetText([IntPtr]$boxes[0].Current.NativeWindowHandle, '3')
[SfWin]::SetText([IntPtr]$boxes[1].Current.NativeWindowHandle, 'the builders of the wall')
[SfWin]::SetText([IntPtr]$boxes[2].Current.NativeWindowHandle, 'Peru')
$saveWin = $win; $win = $edit; Shot 'place_edit.png'; $win = $saveWin
PressIn $edit $L.Ok
Start-Sleep -Milliseconds 800
$saveWin = $win; $win = $manager; Shot 'added_places.png'; $win = $saveWin
PressIn $manager $L.Close
$withAdded = WaitSummary $plain
"SUMMARY ADDED $withAdded"
if (-not (Test-Path $addedFile)) { throw 'the added place was not saved' }
"ADDED $((Get-Content $addedFile -Encoding UTF8 | Select-Object -Last 1))"
Shot 'with_added.png'

# Hide the first place: the list shrinks by one and settings.xml names it.
FirstRow
Press $L.HideSel
$afterHide = WaitSummary $withAdded
"SUMMARY HIDDEN $afterHide"
Check 'hiding removes one row' ((ShownCount $afterHide) -eq (ShownCount $withAdded) - 1) $afterHide
Check 'summary counts the hidden place' ($afterHide -match "1 $($L.Hidden)\.$") $afterHide
Check 'settings name the hidden place' ((Get-Content $settings -Raw) -match '<Place>[a-z]+\|[A-Z0-9]{3} ') ''
Check 'Show hidden tick box says (1)' ((Matching $L.ShowHidden).Current.Name -match '\(1\)$') ''
Shot 'hidden.png'

# Show hidden places: the row is back (in grey italic); make it current and show it again.
[SfWin]::PressButton([IntPtr](Matching $L.ShowHidden).Current.NativeWindowHandle)
$withHidden = WaitSummary $afterHide
"SUMMARY SHOW HIDDEN $withHidden"
Check 'Show hidden lists it again' ((ShownCount $withHidden) -eq (ShownCount $withAdded)) $withHidden
Check 'settings keep ShowHidden' ((Get-Content $settings -Raw) -match '<ShowHidden>Yes</ShowHidden>') ''
FirstRow
Shot 'show_hidden.png'
Press $L.Unhide
$unhidden = WaitSummary $withHidden
"SUMMARY UNHIDDEN $unhidden"
Check 'nothing hidden after Show again' ($unhidden -notmatch $L.Hidden -and (ShownCount $unhidden) -eq (ShownCount $withAdded)) $unhidden
Check 'settings no longer name it' ((Get-Content $settings -Raw) -notmatch '<Place>') ''
[SfWin]::PressButton([IntPtr](Matching $L.ShowHidden).Current.NativeWindowHandle)
Start-Sleep -Milliseconds 500

# Hide a place again, to check after reopening that it stays hidden.
FirstRow
Press $L.HideSel
$oneHidden = WaitSummary $unhidden

# Every publication again, formatted places too, so that Minor divisions are listed.
foreach ($p in 'BFC', 'CEV', 'GNB', 'NVI', 'NLT', 'TEV', $L.Quotations) { Press $p; Start-Sleep -Milliseconds 300 }
Press $L.Hide
Start-Sleep -Milliseconds 300
$null = WaitSummary $oneHidden
Start-Sleep -Milliseconds 1500   # let every refill finish
$beforeKinds = Summary
"SUMMARY ALL AGAIN $beforeKinds"

# Kinds chooser: untick "Minor divisions" (item 15 in Categories.All).
$kindsName = (Matching $L.Kinds).Current.Name
Check 'Kinds button shows all kinds' ($kindsName -match '18') $kindsName
[SfWin]::PressButton([IntPtr](Matching $L.Kinds).Current.NativeWindowHandle)
$chooser = TopWindow $L.KindsTitle
$listEl = @($chooser.FindAll($Scope::Descendants, [Windows.Automation.Condition]::TrueCondition)) | Where-Object { $_.Current.ClassName -match 'LISTBOX' } | Select-Object -First 1
$lh = [IntPtr]$listEl.Current.NativeWindowHandle
Check 'chooser lists 18 kinds' ([SfWin]::ListCount($lh) -eq 18) "$([SfWin]::ListCount($lh))"
[SfWin]::ListClickItem($lh, 15)
Start-Sleep -Milliseconds 500
$saveWin = $win; $win = $chooser; Shot 'kinds.png'; $win = $saveWin
PressIn $chooser $L.Ok
$noDivisions = WaitSummary $beforeKinds
"SUMMARY NO DIVISIONS $noDivisions"
Check 'fewer places without Minor divisions' ((ShownCount $noDivisions) -lt (ShownCount $beforeKinds)) $noDivisions
Check 'Kinds button says 17' ((Matching $L.Kinds).Current.Name -match '17') (Matching $L.Kinds).Current.Name
Check 'settings exclude division' ((Get-Content $settings -Raw) -match '<ExcludedCategories>[^<]*division') ''
Shot 'no_divisions.png'

Press $L.Close
if (-not $proc.WaitForExit(10000)) { $proc.Kill(); throw 'window did not close' }
$proc.StandardOutput.ReadToEnd()

# Open the window again: the hidden place and the kinds are kept.
StartWindow
$again = WaitSummary $null
"SUMMARY REOPENED $again"
Check 'reopened with the same list' ($again -eq $noDivisions) "$again / $noDivisions"
Check 'reopened Kinds button says 17' ((Matching $L.Kinds).Current.Name -match '17') ''
Press $L.Close
if (-not $proc.WaitForExit(10000)) { $proc.Kill(); throw 'window did not close' }

if ($script:fails -gt 0) { throw "$($script:fails) check(s) failed" }
'OK'
