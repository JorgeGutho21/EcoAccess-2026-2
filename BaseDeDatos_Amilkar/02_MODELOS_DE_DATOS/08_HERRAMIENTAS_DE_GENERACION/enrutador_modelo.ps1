<#
  Enrutador ortogonal por "pasillos" para los diagramas del modelo relacional.
  Idea (explicada simple): las tablas se ponen en una cuadrícula (filas x columnas).
  Entre filas y entre columnas quedan PASILLOS vacíos. Cada línea (FK) sale de un
  borde de la tabla hija, viaja SOLO por pasillos (nunca por encima de una tabla) y
  entra por un borde de la tabla padre. Dentro de cada pasillo cada línea recibe su
  propio "carril", así dos líneas nunca quedan una encima de la otra.
  Se usa desde generar_relacional_ordenado.ps1 (SVG) y aplicar_disposicion_dmd.ps1 (.dmd).
#>

function Invoke-Enrutar {
  param(
    [System.Collections.IDictionary]$Cajas,   # nombre -> @{ w; h; r; c }
    [object[]]$Aristas,                       # @{ hija; padre; nombre }
    [int]$Sep = 9, [int]$Margen = 16, [int]$PasilloMin = 34, [int]$Banda = 0
  )
  $R = 0; $C = 0
  foreach ($b in $Cajas.Values) { $R = [math]::Max($R, $b.r + 1); $C = [math]::Max($C, $b.c + 1) }
  $colW = @(0) * $C; $rowH = @(0) * $R
  foreach ($b in $Cajas.Values) { $colW[$b.c] = [math]::Max($colW[$b.c], $b.w); $rowH[$b.r] = [math]::Max($rowH[$b.r], $b.h + $Banda) }
  for ($i = 0; $i -lt $C; $i++) { if ($colW[$i] -eq 0) { $colW[$i] = 120 } }
  for ($i = 0; $i -lt $R; $i++) { if ($rowH[$i] -eq 0) { $rowH[$i] = 60 } }
  $hW = @($PasilloMin) * ($R + 1); $vW = @($PasilloMin) * ($C + 1)

  # --- tipo de ruta y lados de cada arista (no depende de coordenadas) ---
  $rutas = New-Object Collections.Generic.List[object]
  foreach ($e in $Aristas) {
    $h = $Cajas[$e.hija]; $p = $Cajas[$e.padre]
    $o = [ordered]@{ e = $e; tipo = ''; ladoH = ''; ladoP = ''; hc1 = -1; hc2 = -1; vc = -1 }
    if ($e.hija -eq $e.padre) { $o.tipo = 'lazo'; $o.ladoH = 'der'; $o.ladoP = 'der'; $o.vc = $h.c + 1 }
    elseif ($h.r -eq $p.r -and [math]::Abs($h.c - $p.c) -eq 1) {
      $o.tipo = 'lado'; $o.vc = [math]::Max($h.c, $p.c)
      if ($p.c -gt $h.c) { $o.ladoH = 'der'; $o.ladoP = 'izq' } else { $o.ladoH = 'izq'; $o.ladoP = 'der' }
    } elseif ($h.c -eq $p.c -and [math]::Abs($h.r - $p.r) -eq 1) {
      $o.tipo = 'directa'
      if ($p.r -lt $h.r) { $o.ladoH = 'arr'; $o.ladoP = 'aba'; $o.hc1 = $h.r } else { $o.ladoH = 'aba'; $o.ladoP = 'arr'; $o.hc1 = $h.r + 1 }
      $o.hc2 = $o.hc1
    } else {
      if ($p.r -lt $h.r) { $o.ladoH = 'arr'; $o.hc1 = $h.r; $o.ladoP = 'aba'; $o.hc2 = $p.r + 1 }
      elseif ($p.r -gt $h.r) { $o.ladoH = 'aba'; $o.hc1 = $h.r + 1; $o.ladoP = 'arr'; $o.hc2 = $p.r }
      else { $o.ladoH = 'arr'; $o.hc1 = $h.r; $o.ladoP = 'arr'; $o.hc2 = $p.r }
      if ($o.hc1 -eq $o.hc2) { $o.tipo = 'directa' }
      else {
        $o.tipo = 'pasillo'
        if ($h.c -lt $p.c) { $o.vc = $p.c } elseif ($h.c -gt $p.c) { $o.vc = $p.c + 1 } else { $o.vc = $p.c + 1 }
      }
    }
    $rutas.Add([pscustomobject]$o)
  }

  $carrilH = @{}; $carrilV = @{}   # clave "ruta#seg" -> coordenada
  $pos = @{}; $final = $false
  $firmaAnt = ''
  for ($iter = 0; $iter -lt 25; $iter++) {
    # coordenadas de columnas/filas
    $X = @(0) * $C; $acc = 0
    for ($i = 0; $i -lt $C; $i++) { $acc += $vW[$i]; $X[$i] = $acc; $acc += $colW[$i] }
    $ancho = $acc + $vW[$C]
    $Y = @(0) * $R; $acc = 0
    for ($i = 0; $i -lt $R; $i++) { $acc += $hW[$i]; $Y[$i] = $acc; $acc += $rowH[$i] }
    $alto = $acc + $hW[$R]
    $vIni = @(0) * ($C + 1); $acc = 0
    for ($i = 0; $i -le $C; $i++) { $vIni[$i] = $acc; $acc += $vW[$i]; if ($i -lt $C) { $acc += $colW[$i] } }
    $hIni = @(0) * ($R + 1); $acc = 0
    for ($i = 0; $i -le $R; $i++) { $hIni[$i] = $acc; $acc += $hW[$i]; if ($i -lt $R) { $acc += $rowH[$i] } }
    foreach ($k in @($Cajas.Keys)) {
      $b = $Cajas[$k]
      $pos[$k] = @{ x = [math]::Round($X[$b.c] + ($colW[$b.c] - $b.w) / 2); y = [math]::Round($Y[$b.r] + $Banda + ($rowH[$b.r] - $Banda - $b.h) / 2); w = $b.w; h = $b.h }
    }
    function CentroV($i) { $vIni[$i] + $vW[$i] / 2 }
    function CentroH($i) { $hIni[$i] + $hW[$i] / 2 }
    # coordenada provisional de carriles
    foreach ($ru in $rutas) {
      $id = [string]$rutas.IndexOf($ru)
      if ($ru.vc -ge 0 -and -not $carrilV.ContainsKey($id)) { $carrilV[$id] = CentroV $ru.vc }
      if ($ru.hc1 -ge 0 -and -not $carrilH.ContainsKey("$id#1")) { $carrilH["$id#1"] = CentroH $ru.hc1 }
      if ($ru.hc2 -ge 0 -and -not $carrilH.ContainsKey("$id#2")) { $carrilH["$id#2"] = CentroH $ru.hc2 }
    }
    # --- puertos: por caja y lado, ordenados según hacia dónde va la línea ---
    $pedidos = @{}
    foreach ($ru in $rutas) {
      $id = $rutas.IndexOf($ru)
      foreach ($extremo in 'H', 'P') {
        $caja = if ($extremo -eq 'H') { $ru.e.hija } else { $ru.e.padre }
        $lado = if ($extremo -eq 'H') { $ru.ladoH } else { $ru.ladoP }
        $otra = if ($extremo -eq 'H') { $pos[$ru.e.padre] } else { $pos[$ru.e.hija] }
        if ($lado -in 'arr', 'aba') {
          $ref = if ($ru.tipo -eq 'pasillo') { $carrilV[[string]$id] } else { $otra.x + $otra.w / 2 }
        } else {
          $ref = if ($ru.tipo -eq 'lazo') { if ($extremo -eq 'H') { $pos[$caja].y } else { $pos[$caja].y + $pos[$caja].h } } else { $otra.y + $otra.h / 2 }
        }
        $k = "$caja|$lado"; if (-not $pedidos.ContainsKey($k)) { $pedidos[$k] = New-Object Collections.Generic.List[object] }
        $pedidos[$k].Add(@{ id = $id; ext = $extremo; ref = $ref })
      }
    }
    $puerto = @{}
    foreach ($k in $pedidos.Keys) {
      $caja, $lado = $k -split '\|'; $b = $pos[$caja]
      $lista = @($pedidos[$k] | Sort-Object { $_.ref })
      $n = $lista.Count
      if ($lado -in 'arr', 'aba') { $ini = $b.x + 14; $largo = $b.w - 28 } else { $ini = $b.y + 30; $largo = $b.h - 40 }
      $paso = [math]::Min(26, $largo / ($n + 1))
      $centro = $ini + $largo / 2
      for ($i = 0; $i -lt $n; $i++) {
        $v = [math]::Round($centro + ($i - ($n - 1) / 2) * $paso)
        # si la línea va casi recta, intentar alinear con el destino
        $puerto["$($lista[$i].id)$($lista[$i].ext)"] = $v
      }
    }
    # --- polilíneas ---
    $polis = @{}
    foreach ($ru in $rutas) {
      $id = $rutas.IndexOf($ru); $sid = [string]$id
      $bh = $pos[$ru.e.hija]; $bp = $pos[$ru.e.padre]
      function Borde($b, $lado, $v) {
        switch ($lado) { 'arr' { @($v, $b.y) } 'aba' { @($v, ($b.y + $b.h)) } 'izq' { @($b.x, $v) } 'der' { @(($b.x + $b.w), $v) } }
      }
      $a = Borde $bh $ru.ladoH $puerto["${id}H"]; $z = Borde $bp $ru.ladoP $puerto["${id}P"]
      switch ($ru.tipo) {
        'lazo' { $vx = $carrilV[$sid]; $pts = @(, $a) + @(, @($vx, $a[1])) + @(, @($vx, $z[1])) + @(, $z) }
        'lado' { $vx = $carrilV[$sid]; $pts = @(, $a) + @(, @($vx, $a[1])) + @(, @($vx, $z[1])) + @(, $z) }
        'directa' {
          if ([math]::Abs($a[0] - $z[0]) -lt 1) { $pts = @(, $a) + @(, $z) }
          else { $ly = $carrilH["$sid#1"]; $pts = @(, $a) + @(, @($a[0], $ly)) + @(, @($z[0], $ly)) + @(, $z) }
        }
        'pasillo' {
          $vx = $carrilV[$sid]; $l1 = $carrilH["$sid#1"]; $l2 = $carrilH["$sid#2"]
          $pts = @(, $a) + @(, @($a[0], $l1)) + @(, @($vx, $l1)) + @(, @($vx, $l2)) + @(, @($z[0], $l2)) + @(, $z)
        }
      }
      $polis[$id] = $pts
    }
    if ($final) { break }
    # --- asignación de carriles por pasillo (coloreo de intervalos) ---
    $segH = @{}; $segV = @{}
    function SegH($cor, $p, $i, $j, $di, $dj) { @{ cor = $cor; a = [math]::Min($p[$i][0], $p[$j][0]); b = [math]::Max($p[$i][0], $p[$j][0]); ex = @(@{ v = $p[$i][0]; d = $di }, @{ v = $p[$j][0]; d = $dj }) } }
    function SegV($cor, $p, $i, $j, $di, $dj) { @{ cor = $cor; a = [math]::Min($p[$i][1], $p[$j][1]); b = [math]::Max($p[$i][1], $p[$j][1]); ex = @(@{ v = $p[$i][1]; d = $di }, @{ v = $p[$j][1]; d = $dj }) } }
    foreach ($ru in $rutas) {
      $id = $rutas.IndexOf($ru); $sid = [string]$id; $p = $polis[$id]
      switch ($ru.tipo) {
        { $_ -in 'lazo', 'lado' } { $segV[$sid] = SegV $ru.vc $p 1 2 ([math]::Sign($p[0][0] - $p[1][0])) ([math]::Sign($p[3][0] - $p[2][0])) }
        'directa' { if ($p.Count -eq 4) { $segH["$sid#1"] = SegH $ru.hc1 $p 1 2 ([math]::Sign($p[0][1] - $p[1][1])) ([math]::Sign($p[3][1] - $p[2][1])) } }
        'pasillo' {
          $segH["$sid#1"] = SegH $ru.hc1 $p 1 2 ([math]::Sign($p[0][1] - $p[1][1])) ([math]::Sign($p[3][1] - $p[2][1]))
          $segH["$sid#2"] = SegH $ru.hc2 $p 3 4 ([math]::Sign($p[2][1] - $p[3][1])) ([math]::Sign($p[5][1] - $p[4][1]))
          $segV[$sid] = SegV $ru.vc $p 2 3 ([math]::Sign($p[1][0] - $p[2][0])) ([math]::Sign($p[4][0] - $p[3][0]))
        }
      }
    }
    # costo de poner s "antes" (arriba / a la izquierda) que t: cruces que se generan
    function Costo($s, $t) {
      $n = 0
      foreach ($e in $t.ex) { if ($e.d -lt 0 -and $e.v -gt $s.a -and $e.v -lt $s.b) { $n++ } }
      foreach ($e in $s.ex) { if ($e.d -gt 0 -and $e.v -gt $t.a -and $e.v -lt $t.b) { $n++ } }
      # dos ramales en la misma coordenada que se enfrentarían: prohibido (quedarían superpuestos)
      foreach ($e in $s.ex) { if ($e.d -gt 0) { foreach ($q in $t.ex) { if ($q.d -lt 0 -and [math]::Abs($e.v - $q.v) -lt 3) { $n += 1000 } } } }
      $n
    }
    $nuevoHW = @($PasilloMin) * ($R + 1); $nuevoVW = @($PasilloMin) * ($C + 1)
    foreach ($tipo in 'H', 'V') {
      $segs = if ($tipo -eq 'H') { $segH } else { $segV }
      $grupos = $segs.GetEnumerator() | Group-Object { $_.Value.cor }
      foreach ($g in $grupos) {
        $cor = [int]$g.Name
        $orden = New-Object Collections.Generic.List[object]
        foreach ($s in ($g.Group | Sort-Object { ($_.Value.a + $_.Value.b) / 2 }, Key)) { $orden.Add($s) }
        # ordenar carriles: búsqueda local (sacar un tramo y reinsertarlo donde genere menos cruces)
        $n0 = $orden.Count; $M = New-Object 'int[,]' $n0, $n0
        $idx = @{}; for ($i = 0; $i -lt $n0; $i++) { $idx[$orden[$i].Key] = $i }
        for ($i = 0; $i -lt $n0; $i++) { for ($j = 0; $j -lt $n0; $j++) { if ($i -ne $j) { $M[$i, $j] = Costo $orden[$i].Value $orden[$j].Value } } }
        $perm = New-Object Collections.Generic.List[int]; for ($i = 0; $i -lt $n0; $i++) { $perm.Add($i) }
        for ($pasada = 0; $pasada -lt 20; $pasada++) {
          $mejoro = $false
          for ($k = 0; $k -lt $n0; $k++) {
            $e = $perm[$k]; $perm.RemoveAt($k)
            $mejorPos = 0; $mejorC = [int]::MaxValue
            for ($p = 0; $p -le $perm.Count; $p++) {
              $costo = 0
              for ($q = 0; $q -lt $perm.Count; $q++) { if ($q -lt $p) { $costo += $M[$perm[$q], $e] } else { $costo += $M[$e, $perm[$q]] } }
              if ($costo -lt $mejorC) { $mejorC = $costo; $mejorPos = $p }
            }
            $perm.Insert($mejorPos, $e); if ($mejorPos -ne $k) { $mejoro = $true }
          }
          if (-not $mejoro) { break }
        }
        $orden = @($perm | ForEach-Object { $orden[$_] })
        # empaquetar: un tramo comparte carril solo con tramos que no se solapan con él
        $asig = @{}; $n = 0
        for ($i = 0; $i -lt $orden.Count; $i++) {
          $s = $orden[$i].Value; $l = 0
          for ($j = 0; $j -lt $i; $j++) {
            $t = $orden[$j].Value
            if (-not ($t.b + 14 -lt $s.a -or $s.b + 14 -lt $t.a)) { $l = [math]::Max($l, $asig[$orden[$j].Key] + 1) }
          }
          $asig[$orden[$i].Key] = $l; $n = [math]::Max($n, $l + 1)
        }
        $anchoCor = [math]::Max($PasilloMin, 2 * $Margen + ($n - 1) * $Sep)
        if ($tipo -eq 'H') { $nuevoHW[$cor] = $anchoCor; $ini = $hIni[$cor] } else { $nuevoVW[$cor] = $anchoCor; $ini = $vIni[$cor] }
        $actual = if ($tipo -eq 'H') { $hW[$cor] } else { $vW[$cor] }
        $off = $ini + ($actual - ($n - 1) * $Sep) / 2
        foreach ($k in $asig.Keys) {
          $v = [math]::Round($off + $asig[$k] * $Sep)
          if ($tipo -eq 'H') { $carrilH[$k] = $v } else { $carrilV[$k] = $v }
        }
      }
    }
    $cambio = $false
    for ($i = 0; $i -le $R; $i++) { if ($nuevoHW[$i] -ne $hW[$i]) { $cambio = $true } }
    for ($i = 0; $i -le $C; $i++) { if ($nuevoVW[$i] -ne $vW[$i]) { $cambio = $true } }
    $hW = $nuevoHW; $vW = $nuevoVW
    $firma = (($carrilH.GetEnumerator() | Sort-Object Key | ForEach-Object { "$($_.Key)=$($_.Value)" }) -join ";") + "|" + (($carrilV.GetEnumerator() | Sort-Object Key | ForEach-Object { "$($_.Key)=$($_.Value)" }) -join ";")
    if (-not $cambio -and $firma -eq $firmaAnt) { $final = $true }
    $firmaAnt = $firma
  }
  # En la vuelta final solo se recalculan posiciones, puertos y polilíneas con los carriles estables.
  [pscustomobject]@{ Iter = $iter; Final = $final; Pos = $pos; Rutas = $rutas; Polis = $polis; Ancho = $ancho; Alto = $alto; X = $X; Y = $Y; ColW = $colW; RowH = $rowH; VW = $vW; HW = $hW }
}

function Test-Enrutado($res) {
  # Comprueba que ningún tramo pasa por encima de otro (colineal y solapado) ni atraviesa una caja ajena.
  $tramos = New-Object Collections.Generic.List[object]
  foreach ($id in $res.Polis.Keys) {
    $p = $res.Polis[$id]
    for ($i = 0; $i -lt $p.Count - 1; $i++) { $tramos.Add(@{ id = $id; x1 = $p[$i][0]; y1 = $p[$i][1]; x2 = $p[$i + 1][0]; y2 = $p[$i + 1][1] }) }
  }
  $solapes = 0; $cruces = 0; $atraviesa = 0; $det = New-Object Collections.Generic.List[string]
  for ($i = 0; $i -lt $tramos.Count; $i++) {
    $a = $tramos[$i]
    for ($j = $i + 1; $j -lt $tramos.Count; $j++) {
      $b = $tramos[$j]; if ($a.id -eq $b.id) { continue }
      $aH = ($a.y1 -eq $a.y2); $bH = ($b.y1 -eq $b.y2)
      if ($aH -and $bH -and $a.y1 -eq $b.y1) {
        if ([math]::Min([math]::Max($a.x1, $a.x2), [math]::Max($b.x1, $b.x2)) - [math]::Max([math]::Min($a.x1, $a.x2), [math]::Min($b.x1, $b.x2)) -gt 0) { $solapes++; $det.Add("$($res.Rutas[$a.id].e.nombre) [$($a.x1),$($a.y1)-$($a.x2),$($a.y2)] / $($res.Rutas[$b.id].e.nombre) [$($b.x1),$($b.y1)-$($b.x2),$($b.y2)]") }
      } elseif (-not $aH -and -not $bH -and $a.x1 -eq $b.x1) {
        if ([math]::Min([math]::Max($a.y1, $a.y2), [math]::Max($b.y1, $b.y2)) - [math]::Max([math]::Min($a.y1, $a.y2), [math]::Min($b.y1, $b.y2)) -gt 0) { $solapes++; $det.Add("$($res.Rutas[$a.id].e.nombre) [$($a.x1),$($a.y1)-$($a.x2),$($a.y2)] / $($res.Rutas[$b.id].e.nombre) [$($b.x1),$($b.y1)-$($b.x2),$($b.y2)]") }
      } elseif ($aH -ne $bH) {
        $h = if ($aH) { $a } else { $b }; $v = if ($aH) { $b } else { $a }
        if ($v.x1 -gt [math]::Min($h.x1, $h.x2) -and $v.x1 -lt [math]::Max($h.x1, $h.x2) -and $h.y1 -gt [math]::Min($v.y1, $v.y2) -and $h.y1 -lt [math]::Max($v.y1, $v.y2)) { $cruces++ }
      }
    }
    foreach ($k in $res.Pos.Keys) {
      $c = $res.Pos[$k]
      $minx = [math]::Min($a.x1, $a.x2); $maxx = [math]::Max($a.x1, $a.x2); $miny = [math]::Min($a.y1, $a.y2); $maxy = [math]::Max($a.y1, $a.y2)
      if ($maxx -gt $c.x + 1 -and $minx -lt $c.x + $c.w - 1 -and $maxy -gt $c.y + 1 -and $miny -lt $c.y + $c.h - 1) { $atraviesa++ }
    }
  }
  [pscustomobject]@{ Detalle = $det; Tramos = $tramos.Count; Solapes = $solapes; Cruces = $cruces; AtraviesaTablas = $atraviesa }
}
