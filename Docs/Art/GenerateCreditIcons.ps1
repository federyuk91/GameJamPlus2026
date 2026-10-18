# Original 16x16 pixel artwork for the menu. Re-run from the Unity project root.
Add-Type -AssemblyName System.Drawing
$iconDirectory = Join-Path (Get-Location) 'Assets/Art/UI/MainMenu/Icons/Credits'
$colors = @{
    N = [System.Drawing.ColorTranslator]::FromHtml('#142B3A')
    R = [System.Drawing.ColorTranslator]::FromHtml('#AE492F')
    C = [System.Drawing.ColorTranslator]::FromHtml('#F7EDD3')
    G = [System.Drawing.ColorTranslator]::FromHtml('#78AD62')
    T = [System.Drawing.ColorTranslator]::FromHtml('#AC9F82')
}
$icons = @{
    'Credits_DesignerProgrammer_Gear' = @(
        '................',
        '......NNNN......',
        '......NRRN......',
        '..NN.NNRRNN.NN..',
        '..NRNNRRRRNNRN..',
        '...NRRRNNRRRN...',
        '.NNNRNNCCNNRNNN.',
        '.NRRRNCCCCNRRRN.',
        '.NRRRNCCCCNRRRN.',
        '.NNNRNNCCNNRNNN.',
        '...NRRRNNRRRN...',
        '..NRNNRRRRNNRN..',
        '..NN.NNRRNN.NN..',
        '......NRRN......',
        '......NNNN......',
        '................'
    )
    'Credits_Artist_Palette' = @(
        '................',
        '.....NNNNNN.....',
        '...NNTTTTTTNN...',
        '..NTTTRRTTTTTN..',
        '.NTTTRRRRTGGTTN.',
        '.NTTTRRRTTGGTTN.',
        'NTTCCCTTTTTTTTTN',
        'NTTCCCTTTTTNNTTN',
        'NTTTTTTTTTNCCNTN',
        'NTTRRTTTTTNCCNTN',
        '.NTRRTTGGTNCCNN.',
        '.NTTTTTGGTTNN...',
        '..NTTTTTTTTN....',
        '...NNTTTTNN.....',
        '.....NNNN.......',
        '................'
    )
    'Credits_PixelArtist_Pencil' = @(
        '................',
        '...........NN...',
        '..........NRRN..',
        '.........NRRRRN.',
        '........NNRRRN..',
        '.......NTTNRN...',
        '......NTCTNN....',
        '.....NTCTTN.....',
        '....NTCTTN......',
        '...NTCTTN.......',
        '..NNCTTN........',
        '..NCCNN.........',
        '.NTCCN..........',
        '.NTTN...........',
        '.NNN............',
        '................'
    )
    'Credits_SoundMusic_Note' = @(
        '................',
        '.......NNNNNNN..',
        '.....NNRRRRRRN..',
        '....NRRRRRRRRN..',
        '....NRRRNNNNNN..',
        '....NRNN....NN..',
        '....NN......NN..',
        '....NN......NN..',
        '....NN......NN..',
        '....NN....NNNN..',
        '..NNNN...NRRRN..',
        '.NRRRN...NRRRN..',
        '.NRRRN....NNN...',
        '..NNN...........',
        '................',
        '................'
    )
}
foreach ($iconEntry in $icons.GetEnumerator()) {
    $bitmap = [System.Drawing.Bitmap]::new(16, 16)
    try {
        for ($row = 0; $row -lt 16; $row++) {
            $pixels = $iconEntry.Value[$row]
            for ($column = 0; $column -lt $pixels.Length; $column++) {
                $pixelKey = [string]$pixels[$column]
                if ($colors.ContainsKey($pixelKey)) { $bitmap.SetPixel($column, $row, $colors[$pixelKey]) }
            }
        }
        $bitmap.Save((Join-Path $iconDirectory ($iconEntry.Key + '.png')), [System.Drawing.Imaging.ImageFormat]::Png)
    }
    finally { $bitmap.Dispose() }
}
