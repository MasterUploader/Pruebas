Revisa este codigo es para el campo ITIPPAGO


if        pos_fina = 0
     c                   eval      pos_fina = 1
     c                   endif
     c                   clear                   pos
 ****c                   EVAL      Pos    = %Scan('<PAYMENT_TYPE_CD>': MSG:
     c                             pos_fina)
     c                   if        pos > 0
 ****c                   eval      pos  += 17
     c                   eval      pos_ini = pos
 ****c                   EVAL      Pos    = %Scan('</PAYMENT_TYPE_CD>' :MSG:
     c                             pos_fina)
     c                   eval      pos_fina = pos
     c                   eval      lent    = pos_fina - pos_ini
     c                   eval      @ITIPPAGO  = %trim(%subst(msg:pos_ini: lent))
     c                   endif
