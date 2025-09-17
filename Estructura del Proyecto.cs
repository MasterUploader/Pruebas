Dado el código viejo, revisa mi código nuevo y valida si falta algo de lógica por agregar.

Código viejo

      *                                                                       
      /Title Genera Lote de ventas y devoluciones
     H*   ======================================================
     H*                   HCBS                                     *
     H*             ©opyright ®egister                               *
     H* AUTOR......:   Cesar Welchez    13/09/2007                  *
     H*                  Lester Ortega     2024                        *
     H* =============Objetivo=================================
      *====================================================================== *
      * Definiciones                                                          *
      *====================================================================== *
     FIADQCTL   UF   E           K DISK    Extfile('BCAH96DTA/IADQCTL')
     FADQ00ARC  IF   E           K DISK    Extfile('BCAH96DTA/ADQ00ARC')
     FADQ01ENT  IF   E           K DISK    Extfile('BCAH96DTA/ADQ01ENT')
     FADQ02COM  IF   E           K DISK    Extfile('BCAH96DTA/ADQ02COM')
     FADQ03TER  IF   E           K DISK    Extfile('BCAH96DTA/ADQ03TER')
     FADQ04LOT  IF   E           K DISK    Extfile('BCAH96DTA/ADQ04LOT')
     FADQ10DET  UF   E           K DISK    Extfile('BCAH96DTA/ADQ10DET')
     FADQ13IMP  IF   E           K DISK    Extfile('BCAH96DTA/ADQ13IMP')
     FADQ90LOT  IF   E           K DISK    Extfile('BCAH96DTA/ADQ90LOT')
     FADQ91TER  IF   E           K DISK    Extfile('BCAH96DTA/ADQ91TER')
     FADQ92COM  IF   E           K DISK    Extfile('BCAH96DTA/ADQ92COM')
     FADQ93ENT  IF   E           K DISK    Extfile('BCAH96DTA/ADQ93ENT')
     FADQ94ARC  IF   E           K DISK    Extfile('BCAH96DTA/ADQ94ARC')
     FCFP801    IF   E           K DISK    Extfile('BNKPRD01/CFP801')
     FPOP801    UF A E           K DISK    Extfile('BNKPRD01/POP801')
     FPOP802    IF A E           K DISK    Extfile('BNKPRD01/POP802')
     FCFP753    IF   E           K DISK    Extfile('BNKPRD01/CFP753')
     FTAP001    IF   E           K DISK    Extfile('BNKPRD01/TAP001')
     FCFP102    IF   E           K DISK    Extfile('BNKPRD01/CFP102')
     FTAP002CT  IF   E           K DISK    Extfile('BNKPRD01/TAP002CT')
     FIADQCOM   IF   E           K DISK    Extfile('BCAH96DTA/IADQCOM')
     FBINACE    IF   E           K DISK    Extfile('HONDUNEW/BINACE')
      *//*AGREGADO EL 21/07/2025 MEJORA POR ECOMMERCE
     FADQCOBRO  UF A E           K DISK    Extfile('BCAH96DTA/ADQCOBRO')
     FADQECTL   UF A E           K DISK    Extfile('BCAH96DTA/ADQECTL')
     FADQMEMH   UF A E           K DISK    Extfile('BCAH96DTA/ADQMEMH')
     FGLC002    IF   E           K DISK    Extfile('BNKPRD01/GLC002')
     FADQERRLOG UF A E           K DISK    Extfile('BCAH96DTA/ADQERRLOG')
     F********************************************
     F*
     D FECTIM          C                   CONST('ICBSUSER/FECTIM')
      *
     d @00FILE         s                   LIKE(A00FILE)
     D*
     d W00FILE         s                   LIKE(A00FILE)
     d*
     d W01FILE         s                   LIKE(A01FILE)
     d W01FIID         s                   LIKE(A01FIID)
     d
     d W02FILE         s                   LIKE(A02FILE)
     d W02FIID         s                   LIKE(A02FIID)
     d W02COME         s                   LIKE(A02COME)
     d
     d W03FILE         s                   LIKE(A03FILE)
     d W03FIID         s                   LIKE(A03FIID)
     d W03COME         s                   LIKE(A03COME)
     d W03TERM         s                   LIKE(A03TERM)
     d
     d W02NACO         s                   LIKE(A02NACO)
     d
     d W04FILE         s                   LIKE(A04FILE)
     d W04FIID         s                   LIKE(A04FIID)
     d W04COME         s                   LIKE(A04COME)
     d W04TERM         s                   LIKE(A04TERM)
     d W04LOTE         s                   LIKE(A04LOTE)
     d*
     d W10FILE         s                   LIKE(A10FILE)
     d W10FIID         s                   LIKE(A10FIID)
     d W10COME         s                   LIKE(A10COME)
     d W10TERM         s                   LIKE(A10TERM)
     d W10LOTE         s                   LIKE(A10LOTE)
     d*
     d W92FILE         s                   LIKE(A92FILE)
     d W92FIID         s                   LIKE(A92FIID)
     d W92COME         s                   LIKE(A92COME)
     d W_A10CTPO       s                   LIKE(A10CTPO)
     D*
     D*neto trn
     d W_A10NETD       s                   LIKE(A10NETD)
     D*mto trn
     d W_A10IMTR       s                   LIKE(A10IMTR)
     d* importe de afiliacion
     d W_A10ICDE       s                   LIKE(A10ICDE)
     d* impuesto sibre venta retenido
     d W_A10IVCD       s                   LIKE(A10IVCD)
     d* mto de devolucion
     d W_A10IGCI       s                   LIKE(A10IGCI)
     d* mto de Liquidacion
     d W_A13MOTO       s                   LIKE(A13MOTO)
     d*
     D SSSSS           DS
     D  FECHA                  1      6  0
      *-
     D fecffd          DS
     d fectt                   1      6  0
     D  mestt                  1      2  0
     D  diatt                  3      4  0
     D  anntt                  5      6  0
     D fecgg           DS
     D  anngg                  1      2  0
     D  mesgg                  3      4  0
     D  diagg                  5      6  0
      *-Campos y constantes
     D CondCtaOri      s              1A
     D fecsys          S              8
     D horasys         S              7
     D MsgNoPerfil     s              1
     D wTSWSEQ         s              5  0
     D WTSWSCC         s              5  0
     D codtrn          s              4
     D TIPO            s              2
     D CANTdb          s              5s 0
     D TotalDB         s             12s 2
     D CANTcr          s              5s 0
     D TotalCR         s             12s 2
     D Monto_Trn       s             12s 2
     D ErrorFlag       s              1A   inz('0')
     D Dispo           s             12s 2
     D W3ADQCODP       s              3a   inz('999')
     D HayConta        s              5s 0
     D wCEPNEM         s              3
     D wCEPDSC         s             18
     D wdmbrch         s              3s 0
     D bco             S              3s 0 inz
     D cif             S             10A   inz
     D t_debito        S             12s 2 inz(0.00)
     D t_credito       S             12s 2 inz(0.00)
     D wwdis           S             12s 2 inz(0.00)
     D Cc_comis10      S             10s 0 inz(198)
     D Cc_comis3       S              3s 0 inz(198)
     D X               s              2  0 inz(0)
     D NOTRACh6        S              6A   INZ('000000')
     D NOTRACn6        S              6S 0 INZ(0)
     D LUTCZch         S              1A   INZ(*BLANKS)
     D LUCAZch         S             10A   INZ(*BLANKS)
     D WCOME           S             15A   INZ(*BLANKS)
     D wdmacctch10     s             10A   inz(*blanks)
     D wdmacct         s             10s 0 inz(*zeros)
     D Total_Comercio  s             15s 2 inz(*zeros)
     D W_ADQNUM        s                   like(ADQNUM)
     D W_ADQNUM_ch     s             10A   inz(*Blanks)
     D W_ADQCNT1       s                   like(ADQCNT1)
     D W_ADQCCO1       s                   LIKE(ADQCCO1)
     D W_ADQCNT2       s                   like(ADQCNT2)
     D W_ADQCCO2       s                   LIKE(ADQCCO2)
     D W_ADQCNT3       s                   like(ADQCNT3)
     D W_ADQCCO3       s                   LIKE(ADQCCO3)
     D W_ADQCNT4       s                   like(ADQCNT4)
     D W_ADQCCO4       s                   LIKE(ADQCCO4)
     D W_ADQCNT5       s                   like(ADQCNT5)
     D W_ADQCCO5       s                   LIKE(ADQCCO5)
     D W_ADQCNT6       s                   like(ADQCNT6)
     D W_ADQCCO6       s                   LIKE(ADQCCO6)
     D W_ADQCNT7       s                   like(ADQCNT7)
     D W_ADQCCO7       s                   LIKE(ADQCCO7)
     D W_ADQCNT8       s                   like(ADQCNT8)
     D W_ADQCCO8       s                   LIKE(ADQCCO8)
     D W_ADQCNT9       s                   like(ADQCNT9)
     D W_ADQCCO9       s                   LIKE(ADQCCO9)
     D W_ADQCNT10      s                   like(ADQCNT10)
     D W_ADQCC10       S                   LIKE(ADQCC10)
     D W_ADQCNT11      s                   like(ADQCNT11)
     D W_ADQCC11       S                   LIKE(ADQCC11)
     D W_ADQCNT12      s                   like(ADQCNT12)
     D W_ADQCC12       S                   LIKE(ADQCC12)
     D W_ADQCNT13      s                   like(ADQCNT13)
     D W_ADQCC13       S                   LIKE(ADQCC13)
     D W_ADQCNT14      s                   like(ADQCNT14)
     D W_ADQCC14       S                   LIKE(ADQCC14)
     D W_ADQCNT15      s                   like(ADQCNT15)
     D W_ADQCC15       S                   LIKE(ADQCC15)
     D* CODIGOS DE TRN
     D W_ADQCTR1       S                   LIKE(ADQCTR1)
     D W_ADQCTR2       S                   LIKE(ADQCTR2)
     D W_ADQCTR3       S                   LIKE(ADQCTR3)
     D W_ADQCTR4       S                   LIKE(ADQCTR4)
     D W_ADQCTR5       S                   LIKE(ADQCTR5)
     D W_ADQCTR6       S                   LIKE(ADQCTR6)
     D W_ADQCTR7       S                   LIKE(ADQCTR7)
     D W_ADQCTR8       S                   LIKE(ADQCTR8)
     D W_ADQCTR9       S                   LIKE(ADQCTR9)
     D W_ADQCTR10      S                   LIKE(ADQCTR10)
     D W_ADQCTR11      S                   LIKE(ADQCTR11)
     D W_ADQCTR12      S                   LIKE(ADQCTR12)
     D W_ADQCTR13      S                   LIKE(ADQCTR13)
     D W_ADQCTR14      S                   LIKE(ADQCTR14)
     D W_ADQCTR15      S                   LIKE(ADQCTR15)

     D W_TSTCC         s                   LIKE(TSTCC)
     D Sum_Net_Trn     s             12S 2 INZ(0.00)
     D Sum_Mto_Trn     s             12S 2 INZ(0.00)
     D Sum_Afi_Trn     s             12S 2 INZ(0.00)
     D T_Sum_Afi_Trn   s             12S 2 INZ(0.00)
     D T_Sum_Afi_Trn2  s             12S 2 INZ(0.00)
     D Sum_Isr_Trn     s             12S 2 INZ(0.00)
     D T_Sum_Isr_Trn   s             12S 2 INZ(0.00)
     D Sum_Dev_8Po     s             12S 2 INZ(0.00)
     D T_Sum_Dev_8Po   s             12S 2 INZ(0.00)
     D Sum_Devolucion  s             12S 2 INZ(0.00)
     D Sum_Avance_Efe  s             12S 2 INZ(0.00)
     D Sum_Liq_Trn     s             12S 2 INZ(0.00)
     D T_Sum_Liq_Trn   s             12S 2 INZ(0.00)
     D Total_calculad  s             12S 2 INZ(0.00)
     D ExisteCtacli    S              1A   INZ(' ')
     D W_TSTACT        S             12S 0 INZ(0)
     D W_TSWSCC        S              5S 0 INZ(0)
     D Concepto        S              3A   INZ(*blanks)
     D DesConcepto     S             11A   INZ(*blanks)
     d Non_Num_Posn    s              2  0  Inz(*zeros)
     d tamaCODP        s              2  0
IPD  d Numbers         S             10    inz('0123456789')
IPD  d BinCha          S              6a   inz(*blanks)
     D                 DS
     D WFEDIAL                 1      6    INZ
     D FAÑOS                   1      2
     D FMESS                   3      4
     D FDIAS                   5      6
     D                 DS
     D wFTSBT                  1      3S 0 INZ(000)
     D wFTSBTcha               1      3
     d*
     D                 DS
     D eDSCDT                  1      9s 0
     D edia                    2      3
     D emes                    4      5
     D eann                    6      9
     D                 DS
     D cYYYYMMDD               1      8
     D NYYYYMMDD               1      8s 0
     D                 DS
     D CFDRCR                  1      1
     D wCFDRCR                 1      1s 0
     D                 DS
     D LUFDL                   1      4s 0
     D wLUFDL                  1      4
     D                 DS
     D LUFPT                   1      4s 0
     D wLUFPT                  1      4
     D                 DS
     D CFTSBR                  1      3s 0
     D wCFTSBR                 1      3
     D                 DS
     D CFTSTE                  1      4s 0
     D wCFTSTE                 1      4
     D                 DS
     D  wCUX1AC                1     12 00
     D wtip                    1      2 00
     D wcta                    3     12 00
      *//* AGREGADO EL 21/07/2025 MEJORA POR ECOMMERCE
     D esEcommerce     S              5S 0 inz(0)
     D W_ADQENUM       s                   like(ADQENUM)
     D W_ADQENUM_ch    s             10A   inz(*Blanks)
     D W_ADQECNT1      s                   like(ADQECNT1)
     D W_ADQECNT5      s                   like(ADQECNT5)
     D W_ADQECCO1      s                   LIKE(ADQECCO1)
     D W_ADQECCO5      s                   LIKE(ADQECCO5)
     D* CODIGOS DE TRN
     D W_ADQECTR1      S                   LIKE(ADQECTR1)
     D W_ADQECTR5      S                   LIKE(ADQECTR5)

      *//*COBROS A LOS COMERCIOS POR ECOMMERCE
     D DESCFUNC        S                   LIKE(ADQCOBRO03)
     D C_TERMINAL      S             16A   INZ('')
     D C_MEMBRESIA     S                   LIKE(ADQCOBRO04)
     D C_STS           S                   LIKE(ADQCOBRO18)
     D $TasaCompra     S                   LIKE(GBBKXR)
     D MonedaUSD       S              3S 0 inz(001)
     D flagCobro       S                   LIKE(ADQMEMH012)
     D descripFlag     S                   LIKE(ADQMEMH017)
     D StsCtaComercio  S              1A   INZ('I')
     D PgmDs         ESDS                  extname(RPG4DS) qualified
     C*
     C     *ENTRY        PLIST
     C                   PARM                    @00FILE          17
     C                   PARM                    PERFIL           13
     C                   PARM                    Nolote            3
     C                   PARM                    Usuario          10
     C                   PARM                    MARCA1            4
     C                   PARM                    MARCA2            4
     C                   PARM                    TIPO              2
     c*
      *                                                                       
      * Rutina principal del programa                                         *
      *                                                                       
      /free
         exsr FecReal;
         exsr VerFecha;
         exsr VerPerfil;
         If   MsgNoPerfil = '0';
           eval Sum_Liq_Trn= 0.00;
           eval T_Sum_Liq_Trn= 0.00;
           eval T_Sum_Isr_Trn = 0.00;
           eval T_Sum_Afi_Trn= 0.00;
           eval T_Sum_Afi_Trn2= 0.00;
           eval Sum_Dev_8Po  = 0.00;
           eval T_Sum_Dev_8Po  = 0.00;
           W00FILE = @00FILE;
           Setll (W00FILE) ADQ00ARC ;
           Reade (W00FILE) ADQ00ARC ;
           Dow not %Eof(ADQ00ARC );

             //  ©Cabecera de archivo
             W01FILE = A00FILE;
             Setll (W01FILE ) ADQ01ENT ;
             Reade (W01FILE ) ADQ01ENT ;
             Dow not %Eof(ADQ01ENT );

             //  ©Encabezado de Comercio
               W02FILE = A01FILE;
               W02FIID = A01FIID;
               Setll ( W02FILE: W02FIID  ) ADQ02COM ;
               Reade ( W02FILE: W02FIID  ) ADQ02COM ;
               Dow not %Eof(ADQ02COM );
               W02NACO =  A02NACO;
               Exsr Controles;

            //* AGREGADO EL 21/07/2025 MEJORA POR ECOMMERCE
               eval C_MEMBRESIA = 0.00;
            //***
               eval Sum_Net_Trn= 0.00;
               eval Sum_Mto_Trn= 0.00;
               eval Sum_Afi_Trn= 0.00;
               eval Sum_Isr_Trn= 0.00;
               eval Sum_Devolucion = 0.00;
               eval Sum_Avance_Efe = 0.00;
               eval Sum_Liq_Trn = 0.00;
               eval Sum_Dev_8Po  = 0.00;

               exsr Reg_Total_Comercio;

             //  ©Encabezado de Terminal
                 W03FILE = A02FILE;
                 W03FIID = A02FIID;
                 W03COME = A02COME;
                 Setll (W03FILE: W03FIID: W03COME ) ADQ03TER ;
                 Reade (W03FILE: W03FIID: W03COME ) ADQ03TER ;
                 Dow not %Eof(ADQ03TER);

            //* AGREGADO EL 21/07/2025 MEJORA POR ECOMMERCE
            //Aqui hacemos validancion si es una terminal virtual
                  exsr ValidaTerminal;
            //***

             //  ©Encabezado de lotes
                   W04FILE = A03FILE;
                   W04FIID = A03FIID;
                   W04COME = A03COME;
                   W04TERM = A03TERM;
                   Setll (W04FILE: W04FIID: W04COME: W04TERM ) ADQ04LOT ;
                   Reade (W04FILE: W04FIID: W04COME: W04TERM ) ADQ04LOT ;
                   Dow not %Eof(ADQ04LOT) ;

             //  ©Registro de Detalle General
                     W10FILE = A04FILE;
                     W10FIID = A04FIID;
                     W10COME = A04COME;
                     W10TERM = A04TERM;
                     W10LOTE = A04LOTE;

                     Setll (W10FILE:W10FIID:W10COME:W10TERM:W10LOTE) ADQ10DET ;
                     Reade (W10FILE:W10FIID:W10COME:W10TERM:W10LOTE) ADQ10DET ;
                     Dow not %Eof(ADQ10DET);
                      If (A10FIDT = MARCA1 OR A10FIDT = MARCA2);
                         A10NUMU = W_ADQNUM;

                         A13FILE = A10FILE; //ARCHIVO
                         A13FIID = A10FIID; //ENTIDAD PROSA EMISOR
                         A13COME = A10COME; //CLAVE COMERCIO
                         A13TERM = A10TERM; //TERMINAL
                         A13LOTE = A10LOTE; //CLAVE DE LOTE
                         A13NUMR = A10NUMR; //NUMERO REFERENCIA
                         chain (A13FILE: A13FIID: A13COME: A13TERM:
                                A13LOTE: A13NUMR) ADQ13IMP;

                         Exsr Totalize;
                         A10CTPO = W_A10CTPO; //CTA POSTEO
                         A10CATC = A02CATC; //FAMILIA DEL COMERCIO
                         A10GICO = A02GICO; //GIRO DEL COMERCIO
                         A10CTRE = 0; //CTA RECHAZO
                         A10STAT = 'Genera_Lot'; //ESTATUS TRANSACCION
                         UPDATE R10DETA;
                      EndIf;
                      Reade (W10FILE:W10FIID:W10COME:W10TERM:W10LOTE) ADQ10DET ;
                     Enddo;

                   Reade (W04FILE: W04FIID: W04COME: W04TERM ) ADQ04LOT ;
                   Enddo;

                 Reade ( W03FILE: W03FIID: W03COME ) ADQ03TER ;
                 Enddo;


          //  ©comparacion de total de comercio con sumatoria detalle

               Total_calculad  =
               (Sum_Net_Trn + Sum_Avance_Efe - Sum_Devolucion);
                   Exsr Ver_Cta;
                   // If ErrorFlag = '0';

                         //  (2)©contabilida debito afiliacion comercio
                         If ExisteCtacli = 'S';
                            IF DMTYP = 1;
                               LUTCZch = '1';
                               CodTrn ='DHAD';        // DEBITO AHO
                            Else;
                               IF DMTYP = 6;
                                  LUTCZch = '6';
                                  CodTrn ='DCAD';      // DEBITO CHK
                               EndIf;
                            EndIf;
                            W_TSTACT = wdmacct  ;
                            W_TSWSCC = 0;
                            W_A10CTPO = wdmacct;
                            Concepto = 'AFI';
                            DesConcepto = 'Db Afil.Com';
                            W_TSTCC = Sum_Afi_trn;
                   //       Exsr DbCliVta ;
                         Else;
                            //  ©contablidad cr Neto Trn contable
                            LUTCZch = ' ';
                            W_TSTACT = W_ADQCNT11;
                            W_TSWSCC = W_ADQCC11 ;
                            W_A10CTPO = W_ADQCNT11;
                            W_TSTCC  = Sum_Afi_trn;
                            Codtrn   =  W_ADQCTR11;    // 0784=Db
                            Concepto = 'AFI';
                            DesConcepto = 'Db Afil.Com';
                            Exsr Debito;
                         Endif;
                      //CREDITOS A CUENTA DE RECHAZO SI CUENTA COMERCIO INACTIVA
                         exsr Credito_comercio;
                         exsr Credito_8_Porciento;

       //  ©contabilida Avance de efectivo por COMERCIO
                     //  ©contabilida Debito por avance de efectivo
                         W_TSTACT = W_ADQCNT3;
                         W_TSWSCC = W_ADQCCO3;
                         Concepto = 'AVA';
                         DesConcepto = 'Db Ava.Efec';
                         W_TSTCC  = Sum_Avance_Efe;
                         Codtrn   =  W_ADQCTR3;      // 0784=Db
                         Exsr Debito;
                     //  ©contabilida credito por avance de efectivo
                         W_TSTACT = W_ADQCNT10;
                         W_TSWSCC = W_ADQCC10;
                         Concepto = 'AVA';
                         DesConcepto = 'Cr Ava.Efec';
                         W_TSTCC = Sum_Avance_Efe;
                         // CodTrn ='CCAD';   // CCAD=Cr antes wel 08/12/15
                         Codtrn    = W_ADQCTR10; // 0783=Cr Despues
                         Exsr Credito;
                   // Endif;

       //* CONTABILIDAD COMISIÓN POR MEMEBRESÍA ÚNICA ECOMMERCE
                if (C_MEMBRESIA <> 0);
                  If ( ExisteCtacli = 'S' AND StsCtaComercio = 'A' );

                    W_ADQNUM_ch = W_ADQENUM_ch;

                    IF DMTYP = 1;
                        LUTCZch = '1'; //Debito a Cta AHO
                        CodTrn ='DHAD';        // DEP.AHO
                    Else;
                        IF DMTYP = 6;
                          LUTCZch = '6'; //Debito a Cta CHK
                          CodTrn ='DCAD';      // DEP CHK
                        EndIf;
                    EndIf;

                    //* Debito por MEMEBRESÍA ÚNICA a COMERCIO
                    W_TSTACT = wdmacct;
                    W_TSWSCC = 0;
                    W_A10CTPO = wdmacct;
                    Concepto = 'MEM';
                    DesConcepto = 'Db MemUnica';
                    W_TSTCC = C_MEMBRESIA;
                    Exsr DbCliVta ;

                  else;

                    W_ADQNUM_ch = W_ADQENUM_ch;

                    //* Debito por MEMEBRESÍA ÚNICA a CTA RECHAZO
                    LUTCZch = '6'; //Debito a Cta Contable:
                    W_TSTACT = W_ADQECNT5;
                    W_TSWSCC = W_ADQECCO5;
                    Concepto = 'MEM';
                    DesConcepto = 'Db MemUnica';
                    W_TSTCC  = C_MEMBRESIA;
                    Codtrn   = W_ADQECTR5;  //'0784'
                    W02NACO = 'MEMBRESÍA ECOM ADQ';
                    Exsr Debito2;
                  Endif;

                  //* Crédito por MEMEBRESÍA ÚNICA a CTA CONTABLE
                    LUTCZch = ' ';
                    W_TSTACT = W_ADQECNT1; //CUENTA CONTABLE MEM UNICA
                    W_TSWSCC = W_ADQECCO1; //CENTRO COSTO CTA MEM UNICA
                    Concepto = 'MEM';
                    DesConcepto = 'Cr MemUnica';
                    W_TSTCC = C_MEMBRESIA; // MONTO DEL LOTE
                    Codtrn    = W_ADQECTR1; // COD TRN 0783=CR MEM UNICA
                    W02NACO = 'MEMBRESÍA ECOM ADQ';
                    Exsr Credito;
                    Exsr CambiaStsCobroMemUnica;
                    Exsr HistoricoCobroEcommerce;
                    W_ADQNUM_ch = %char(W_ADQNUM);
                ENDIF;
               Reade ( W02FILE: W02FIID ) ADQ02COM ;
               Enddo;

             Reade (W01FILE) ADQ01ENT ;
             Enddo;

           Reade (W00FILE) ADQ00ARC ;
           Enddo;


                     //  ©contabilida Debito Neto de liquidacion comerci
                         LUTCZch = ' ';
                         W_TSTACT = W_ADQCNT1;
                         W_TSWSCC = W_ADQCCO1;
                         Concepto = 'VTA';
                         DesConcepto = 'Db Net.Liq1';
                         W_TSTCC  = T_Sum_Liq_Trn ;
                         Codtrn   = W_ADQCTR1;  //'0784'   0783=Cr 0784=Db
                         W02NACO = 'Total Neto Db liquidacion come';
                         Exsr Debito;
                     //  ©contabilida Credito Neto de liquidacion comerci
                         LUTCZch = ' ';
                         W_TSTACT = W_ADQCNT2;
                         W_TSWSCC = W_ADQCCO2;
                         Concepto = 'VTA';
                         DesConcepto = 'Cr Net.Liq2';
                         W_TSTCC  = T_Sum_Liq_Trn;
                         Codtrn =  W_ADQCTR2;  // '0783';  0783=Cr 0784=Db
                         W02NACO = 'Total Neto Cr liquidacion come';
                         Exsr Credito;
                     //  ©contabilida Debito Neto de liquidacion come rci
                         LUTCZch = ' ';
                         W_TSTACT = W_ADQCNT7;
                         W_TSWSCC = W_ADQCCO7;
                         Concepto = 'VTA';
                         DesConcepto = 'Db Net.Liq3';
                         W_TSTCC  = T_Sum_Liq_Trn ;
                         Codtrn   = W_ADQCTR7;      //     0784=Db
                         W02NACO = 'Total Neto Db liquidacion come';
                         Exsr Debito ;
        // 27/02/2017  //  ©contabilida total ISR
                         W_TSTACT = W_ADQCNT5;
                         W_TSWSCC = W_ADQCCO5;
                         W_TSTCC  = T_Sum_Isr_Trn;
                         Codtrn   =  W_ADQCTR5;  //  0783=cr
                         Concepto = 'ISR';
                         DesConcepto = 'Cr Net.ISR ';
                         W02NACO = 'Total CR Imp. Sobre la Renta ';
                         Exsr Credito  ;
                     //  ©contabilida debito 8 porciento impuesto
                         LUTCZch = ' ';
                         W_TSTACT = W_ADQCNT6;
                         W_TSWSCC = W_ADQCCO6;
                         Concepto = 'VTA';
                         DesConcepto = 'Dev.8%.Come';
                         W_TSTCC  = T_Sum_Dev_8Po;
                         Codtrn   =  W_ADQCTR6;   //  0784=Db
                         W02NACO = 'Total Neto Db 8% Sobre Impuest';
                         Exsr Debito ;
                         //  ©contablidad cr afiliacion trn nuestra
                         LUTCZch = ' ';
                         W_TSTACT = W_ADQCNT9;
                         Exsr VerCcCome;
                         W_TSWSCC = Cc_comis3;
                         W_TSTCC  = T_Sum_Afi_trn;
                         Codtrn = W_ADQCTR9;        // 0783=Cr
                         Concepto = 'AFI';
                         DesConcepto = 'Cr Net.Afil';
                         W02NACO = 'Total CR Afiliacion          ';
                         Exsr Credito;
                         //  ©contablidad cr afiliacion trn ot bancos
                         LUTCZch = ' ';
                         W_TSTACT = W_ADQCNT4;
                         W_TSWSCC = W_ADQCCO4;
                         W_TSTCC  = T_Sum_Afi_trn2;
                         Codtrn   = W_ADQCTR4;    // 0783=Cr
                         Concepto = 'AFI';
                         DesConcepto = 'Cr Net.Afil';
                         W02NACO = 'Total CR Afiliacion          ';
                         Exsr Credito;

           exsr UpdHdrLote;

         Endif;
         *inlr=*on;

       BEGSR VerFileTrn;
             CFBK=001;
             CFTCNO=CodTrn;
             chain (CFBK:  CFTCNO) CFP753;
             If %Found(CFP753);
                TSTSAP =   CFAPPL;
                TSWDRC =   CFDRCR;
                exsr acumula;
             endif;
       ENDSR;

       BEGSR Acumula;
          If wCFDRCR > 5;   // Process Debit
               CANTdb = CANTdb + 1;
               TotalDB = TotalDB + TSTCC;
               t_debito= t_debito + TSTCC;
          Else;
            If wCFDRCR <= 5;   // Process Credit
               CANTcr = CANTcr + 1;
               TotalCR = TotalCR + TSTCC;
               t_credito=t_credito + TSTCC;
            Endif;
          Endif;
       ENDSR;
       Begsr VerPerfil;
           MsgNoPerfil = '1';
           CFTSBK=001;
           CFTSKY=PERFIL;
           Chain (CFTSBK:CFTSKY) CFP801;
           If %Found(CFP801);
              MsgNoPerfil = '0';
              exsr VerUltlote;
           ENDIF;
           If MsgNoPerfil = '1';
              Dsply 'No existe perfil';
           Endif;
         EndSr;

         Begsr VerUltlote;
             wFTSBT = 000;
             FTTSBK=001;
             FTTSKY=PERFIL;
             Setll ( FTTSBK: FTTSKY ) pop801;
             Reade ( FTTSBK: FTTSKY  ) pop801;
             Dow not %Eof(pop801);
                      wFTSBT = FTSBT ;
             Reade ( FTTSBK: FTTSKY  ) pop801;
             Enddo;
             Exsr NuevoLote;
         EndSr;


         Begsr NuevoLote;
             clear POP8011;
             wFTSBT = wFTSBT + 1;
             Nolote =wFTSBTcha;
             FTTSBK=001;
             FTTSKY=PERFIL;
             FTSBT = wFTSBT;
             FTTSST = 02;
             FTTSOR =Usuario;
             FTTSDT =  DSDT;
             write Pop8011;
             wTSWSEQ = 0;
         EndSr;

         Begsr UpdHdrLote;
             FTTSBK=001;
             FTTSKY=PERFIL;
             FTSBT = wFTSBT;
             Chain (FTTSBK: FTTSKY: FTSBT ) pop801;
             If %Found(pop801);
                FTTSDI = CANTdb;
                FTTSID = totalDB;
                FTTSCI = CANTcr;
                FTTSIC = totalCR;
                Update pop8011;
             Endif;
         EndSr;

         Begsr VerFecha;
           DSBK =001;
           Chain (DSBK) TAP001;
           If %Found(TAP001);
              eDSCDT = DSCDT;
              cYYYYMMDD = eann + emes +  edia ;
           ENDIF;
         EndSr;

      *-Postear credito a cuenta del cliente
         BEGSR CrCliVta ;
           clear pop8021;
           TSBK    = 001;
           TSTSKY  = PERFIL ;
           TSBTCH  =  wFTSBT;
           wTSWSEQ = wTSWSEQ + 1;
           TSWSEQ = wTSWSEQ;
           TSTOVR =   'S';
           TSTTDT =  nYYYYMMDD;             // fecha efectiva
           TSTACT = W_TSTACT;
           TSWSCC = W_TSWSCC;
           TSWTCD = CODTRN;
           TSTCC =  W_TSTCC;
           EXSR VerFileTrn;
           NOTRACn6=%dec(NOTRACh6:6:0);
           TSTCN1 = NOTRACn6;
           TSTAL1 = ' ';
           TSTAL2 = ' ';
           TSTAL3 = ' ';
           TSTAL1 = W02NACO;
           WCOME  = %CHAR(A02COME);
           TSTAL2 = WCOME  + '-' + A02FEDA;
           %subst(TSTAL3:1:1) = '&' ;
           %subst(TSTAL3:2:3) = Concepto;
           %subst(TSTAL3:5:1) = '&' ;
           %subst(TSTAL3:6:10) = W_ADQNUM_ch;
           %subst(TSTAL3:16:11) = DesConcepto;
           %subst(TSTAL3:29:2) = '||';
           hayconta=hayconta + 1;
             IF TSTCC > 0;
              write pop8021;
             Endif;
         ENDSR;
      *-Postear debito a cuenta del cliente
         BEGSR DbCliVta ;
           clear pop8021;
           TSBK    = 001;
           TSTSKY  = PERFIL ;
           TSBTCH  =  wFTSBT;
           wTSWSEQ = wTSWSEQ + 1;
           TSWSEQ = wTSWSEQ;
           TSTOVR =   'S';
           TSTTDT =  nYYYYMMDD;             // fecha efectiva
           TSTACT = W_TSTACT;
           TSWSCC = W_TSWSCC;
           TSWTCD = CODTRN;
           TSTCC =  W_TSTCC;
           EXSR VerFileTrn;
           TSTCN1 = 0;
           TSTAL1 = ' ';
           TSTAL2 = ' ';
           TSTAL3 = ' ';
           TSTAL1 = W02NACO;
           WCOME  = %CHAR(A02COME);
           TSTAL2 = WCOME  + '-' + A02FEDA;
           %subst(TSTAL3:1:1) = '&' ;
           %subst(TSTAL3:2:3) = Concepto;
           %subst(TSTAL3:5:1) = '&' ;
           %subst(TSTAL3:6:10) = W_ADQNUM_ch;
           %subst(TSTAL3:16:11) = DesConcepto;
           %subst(TSTAL3:29:2) = '||';
           hayconta=hayconta + 1;
             IF TSTCC > 0;
              Write pop8021;
             ENDIF;
         ENDSR;

         BEGSR Credito ;
           clear pop8021;
           TSBK    = 001;
           TSTSKY  = PERFIL ;
           TSBTCH  =  wFTSBT;
           wTSWSEQ = wTSWSEQ + 1;
           TSWSEQ  = wTSWSEQ;
           TSTOVR  =   'S';
           TSTTDT  =  nYYYYMMDD;             // fecha efectiva
           TSTACT  =  W_TSTACT;
           TSWSCC  =  W_TSWSCC;
           TSWTCD  = CODTRN;
           TSTCC   = W_TSTCC;
           EXSR VerFileTrn;
           TSTCN1 = 0;
           TSTAL1 = ' ';
           TSTAL2 = ' ';
           TSTAL3 = ' ';
           TSTAL1 = W02NACO;
           WCOME  = %CHAR(A02COME);
           TSTAL2 = WCOME  + '-' + A02FEDA;
           %subst(TSTAL3:1:1) = '&' ;
           %subst(TSTAL3:2:3) = Concepto;
           %subst(TSTAL3:5:1) = '&' ;
           %subst(TSTAL3:6:10) = W_ADQNUM_ch;
           %subst(TSTAL3:16:11) =  DesConcepto;
           %subst(TSTAL3:29:2) = '||';
           hayconta=hayconta + 1;
              IF TSTCC > 0;
              write pop8021;
              EndIf;
         ENDSR;
         BEGSR Debito;
           clear pop8021;
           TSBK    = 001;
           TSTSKY  = PERFIL ;
           TSBTCH  =  wFTSBT;
           wTSWSEQ = wTSWSEQ + 1;
           TSWSEQ  = wTSWSEQ;
           TSTOVR  =   'S';
           TSTTDT  =  nYYYYMMDD;             // fecha efectiva
           TSTACT  = W_TSTACT;
           TSWSCC  = W_TSWSCC;
           LUTCZch = '6';
           TSWTCD  = CODTRN;
           TSTCC   =  W_TSTCC;
           LUCAZch = wdmacctch10;
           EXSR VerFileTrn;
           NOTRACn6=%dec(NOTRACh6:6:0);
           TSTCN1  = NOTRACn6;
           TSTAL1  = ' ';
           TSTAL2  = ' ';
           TSTAL3  = ' ';
           TSTAL1 = W02NACO;
           WCOME  = %CHAR(A02COME);
           TSTAL2 = WCOME  + '-' + A02FEDA + '-'+ LUTCZch + '-' + LUCAZch;
           %subst(TSTAL3:1:1) = '&' ;
           %subst(TSTAL3:2:3) = Concepto;
           %subst(TSTAL3:5:1) = '&' ;
           %subst(TSTAL3:6:10) = W_ADQNUM_ch;
           %subst(TSTAL3:16:11) =  DesConcepto;
           %subst(TSTAL3:29:2) = '||';
           hayconta=hayconta + 1;
              IF TSTCC > 0;
              Write pop8021;
              EndIf;
         ENDSR;

         BEGSR Debito2;
           clear pop8021;
           TSBK    = 001;
           TSTSKY  = PERFIL ;
           TSBTCH  =  wFTSBT;
           wTSWSEQ = wTSWSEQ + 1;
           TSWSEQ  = wTSWSEQ;
           TSTOVR  =   'S';
           TSTTDT  =  nYYYYMMDD;             // fecha efectiva
           TSTACT  = W_TSTACT;
           TSWSCC  = W_TSWSCC;
           //LUTCZch = '6';
           TSWTCD  = CODTRN;
           TSTCC   =  W_TSTCC;
           LUCAZch = wdmacctch10;
           EXSR VerFileTrn;
           NOTRACn6=%dec(NOTRACh6:6:0);
           TSTCN1  = NOTRACn6;
           TSTAL1  = ' ';
           TSTAL2  = ' ';
           TSTAL3  = ' ';
           TSTAL1 = W02NACO;
           WCOME  = %CHAR(A02COME);
           if (LUTCZch <> *BLANKS AND LUCAZch <> *BLANKS);
            TSTAL2 = WCOME  + '-' + A02FEDA + '-'+ LUTCZch + '-' + LUCAZch;
           else;
            TSTAL2 = WCOME  + '-' + A02FEDA;
           endif;
           %subst(TSTAL3:1:1) = '&' ;
           %subst(TSTAL3:2:3) = Concepto;
           %subst(TSTAL3:5:1) = '&' ;
           %subst(TSTAL3:6:10) = W_ADQNUM_ch;
           %subst(TSTAL3:16:11) =  DesConcepto;
           %subst(TSTAL3:29:2) = '||';
           hayconta=hayconta + 1;
              IF TSTCC > 0;
              Write pop8021;
              EndIf;
         ENDSR;

         Begsr Reg_Total_Comercio;
           Total_Comercio = 0.00;
           W92FILE = A02FILE;
           W92FIID = A02FIID;
           W92COME = A02COME;
           chain (W92FILE: W92FIID: W92COME) ADQ92COM;
           IF %FOUND(ADQ92COM);
              Total_Comercio = A92TODE;
           ELSE;
              DSPLY 'NO HAY TOTALES DEL COMERCIO';
              ErrorFlag = '1';
              Total_Comercio = 0.00;
           ENDIF;
         ENDSR;

         Begsr Ver_Cta;
           wdmacctch10 = %subst(A02CTDE: 1: 10);
           wdmacct    = %DEC(wdmacctch10 :10:0);
           chain (wdmacct) tap002CT;
           IF %FOUND(TAP002CT);
              ExisteCtacli = 'S';
           ELSE;
              ExisteCtacli = 'N';
           ENDIF;

           IF (DMSTAT <>  '1' and DMSTAT <>  '6');
              //*Estatus de la cuenta invalido
              StsCtaComercio = 'I';
            ELSE;
              StsCtaComercio = 'A';
              LUCAZch = wdmacctch10;
              IF DMTYP = 1;
                LUTCZch = '1';
                CodTrn ='DHAD';        // DEBITO AHO
              Elseif (DMTYP = 6);
                LUTCZch = '6';
                CodTrn ='DCAD';      // DEBITO CHK
              EndIf;
            ENDIF;
         ENDSR;

         BEGSR Controles;
           chain (TIPO) IADQCTL;
           if %found(IADQCTL);
              ADQNUM = ADQNUM + 1;
              W_ADQNUM = ADQNUM;
              W_ADQNUM_ch = %char(W_ADQNUM);
              W_ADQCNT1 = ADQCNT1;
              W_ADQCCO1 = ADQCCO1;
              W_ADQCNT2 = ADQCNT2;
              W_ADQCCO2 = ADQCCO2;
              W_ADQCNT3 = ADQCNT3;
              W_ADQCCO3 = ADQCCO3;
              W_ADQCNT4 = ADQCNT4;
              W_ADQCCO4 = ADQCCO4;
              W_ADQCNT5 = ADQCNT5;
              W_ADQCCO5 = ADQCCO5;
              W_ADQCNT6 = ADQCNT6;
              W_ADQCCO6 = ADQCCO6;
              W_ADQCNT7 = ADQCNT7;
              W_ADQCCO7 = ADQCCO7;
              W_ADQCNT8 = ADQCNT8;
              W_ADQCCO8 = ADQCCO8;
              W_ADQCNT9 = ADQCNT9;
              W_ADQCCO9 = ADQCCO9;
              W_ADQCNT10= ADQCNT10;
              W_ADQCC10= ADQCC10;
              W_ADQCNT11= ADQCNT11;
              W_ADQCC11= ADQCC11;
              W_ADQCNT12= ADQCNT12; //Cta rechazo Cre
              W_ADQCC12= ADQCC12; //C.C rechazo Cre
              W_ADQCNT13= ADQCNT13; //Cta rechazo Deb
              W_ADQCC13 = ADQCC13; //C.C rechazo Deb
              W_ADQCNT14= ADQCNT14;
              W_ADQCC14= ADQCC14;
              W_ADQCNT15= ADQCNT15;
              W_ADQCC15= ADQCC15;
            // CODIGOS DE TRANSACCIO
              W_ADQCTR1 = ADQCTR1;
              W_ADQCTR2 = ADQCTR2;
              W_ADQCTR3 = ADQCTR3;
              W_ADQCTR4 = ADQCTR4;
              W_ADQCTR5 = ADQCTR5;
              W_ADQCTR6 = ADQCTR6;
              W_ADQCTR7 = ADQCTR7;
              W_ADQCTR8 = ADQCTR8;
              W_ADQCTR9 = ADQCTR9;
              W_ADQCTR10= ADQCTR10;
              W_ADQCTR11= ADQCTR11;
              W_ADQCTR12= ADQCTR12; //TRN rechazo Cre
              W_ADQCTR13= ADQCTR13; //TRN rechazo Deb
              W_ADQCTR14= ADQCTR14;
              W_ADQCTR15= ADQCTR15;
              Update GXADQCTL;
           else;
             IF not %found(IADQCTL);
                DSPLY 'No hay reg control contable';
                return;
             ENDIF;
           Endif;
         EndSr;

         begsr Totalize;
             //         © 01 Ventas
             //         © 02 Disposiciones en Efectiv
             //         © 20 Pagos
             //         © 21 Devoluciones            ?
             //         © 22 Prepopina y Pospropina
             //         © 23 Check In And Check Out
                       If A10NETD < 0;    // neto deposito
                          W_A10NETD = A10NETD * -1;
                       Else;
                          W_A10NETD = A10NETD;
                       Endif;
                       If A10IVCD < 0;    // Impuestos
                          W_A10IVCD = A10IVCD * -1;
                       Else;
                          W_A10IVCD = A10IVCD;
                       Endif;
                       If A10IMTR < 0;    // mto de transaccion
                          W_A10IMTR = A10IMTR * -1;
                       Else;
                          W_A10IMTR = A10IMTR;
                       Endif;
                       If A10ICDE < 0;    // mto de afiliacion
                          W_A10ICDE = A10ICDE * -1;
                       Else;
                          W_A10ICDE = A10ICDE;
                       Endif;
                       If A10IGCI < 0;    // mto de afiliacion
                          W_A10IGCI = A10IGCI * -1;
                       Else;
                          W_A10IGCI = A10IGCI;
                       Endif;
                       If A13MOTO < 0;    // mto de Liquidacion
                          W_A13MOTO = A13MOTO * -1;
                       Else;
                          W_A13MOTO = A13MOTO;
                       Endif;
                       If A10CLAT = 01; // 
       // 2017/01/27                    //  ©sumarizacion Neto Trn
                         eval Sum_Net_Trn = Sum_Net_Trn + W_A10NETD ;
                                        //  ©sumarizacion de Monto Trn
                         eval Sum_Mto_Trn = Sum_Mto_Trn + W_A10IMTR ;
                                        //  ©sumarizacion Mto afiliacion
                         eval Sum_Afi_Trn= Sum_Afi_trn + W_A10ICDE ;
                         BinCha = %subst(A10NCTA:1:6);
                         ABINCO = %dec(BinCha:6:0);
                         chain ( ABINCO ) BINACE;
                         If %Found(BINACE);
                            // comision por tasa de intercambio nuetra
                            Eval T_Sum_Afi_Trn= T_Sum_Afi_trn + W_A10ICDE;
                         Else;
                            // comision por tasa de intercambio ot bancos
                            Eval T_Sum_Afi_Trn2= T_Sum_Afi_trn2 + W_A10ICDE;
                         EndIf;
                                        //  ©sumarizacion de ISR
                         eval Sum_Isr_Trn= Sum_Isr_Trn + W_A10IVCD ;
                         eval T_Sum_Isr_Trn= T_Sum_Isr_Trn + W_A10IVCD ;
                                        //  ©sumarizacion de 8% Dev
                         eval Sum_Dev_8Po  = Sum_Dev_8Po   + W_A10IGCI ;
                         eval T_Sum_Dev_8Po  = T_Sum_Dev_8Po   + W_A10IGCI ;
                         //  ©sumarizacion Total Liquidacion POR COMERCIO
                         eval Sum_Liq_Trn= Sum_Liq_Trn + W_A13MOTO ;
                                        //  ©sumarizacion Total Liquidacion
                         eval T_Sum_Liq_Trn= T_Sum_Liq_Trn + W_A13MOTO ;

                       Else;
                       If A10CLAT = 02; //  ©Avance de efectivo
                         eval Sum_Avance_Efe = Sum_Avance_Efe + W_A10IMTR ;
                       Else;
                       If A10CLAT = 21; //  ©sumarizacion devoluciones
                         eval Sum_Devolucion = Sum_Devolucion + W_A10NETD;
                       Endif;
                       Endif;
                       Endif;
         EndSr;

         Begsr Credito_comercio;
                        //(2)©contabilida Credito a comercio
                         If ( ExisteCtacli = 'S' AND StsCtaComercio = 'A' );
                            IF DMTYP = 1;
                               LUTCZch = '1';
                               CodTrn ='CHAD';        // DEP.AHO
                            Else;
                               IF DMTYP = 6;
                                  LUTCZch = '6';
                                  CodTrn ='CCAD';      // DEP CHK
                               EndIf;
                            EndIf;
                            W_TSTACT = wdmacct  ;
                            W_TSWSCC = 0;
                            Concepto = 'VTA';
                            DesConcepto = 'Cr Comercio';
                            //weldelewl
                            W_TSTCC  = Sum_Net_Trn;
                            Exsr CrCliVta ;
                         Else;
                            //  ©contablidad Credito a comercio
                            LUTCZch = ' ';
                            W_TSTACT = W_ADQCNT8;
                            W_TSWSCC = W_ADQCCO8;
                            W_TSTCC  = Sum_Net_Trn;
                            Codtrn   = W_ADQCTR8;      // 0783=Cr no cta cli
                            Concepto = 'VTA';
                            DesConcepto = 'Cr Comercio';
                            Exsr Credito;
                         Endif;
         EndSr;


          Begsr Credito_8_Porciento;
                        //(2)©contabilida Credito a comercio
                         If ( ExisteCtacli = 'S' AND StsCtaComercio = 'A' );
                            IF DMTYP = 1;
                               LUTCZch = '1';
                               CodTrn ='CHAD';        // DEP.AHO
                            Else;
                               IF DMTYP = 6;
                                  LUTCZch = '6';
                                  CodTrn ='CCAD';      // DEP CHK
                               EndIf;
                            EndIf;
                            W_TSTACT = wdmacct  ;
                            W_TSWSCC = 0;
                            Concepto = 'VTA';
                            DesConcepto = 'Dev.8%.Come';
                            //weldelewl
                            W_TSTCC  = Sum_Dev_8Po;
                            Exsr CrCliVta ;
                         Else;
                            //  ©contablidad Credito a comercio
                            LUTCZch = ' ';
                            W_TSTACT = W_ADQCNT8;
                            W_TSWSCC = W_ADQCCO8;
                            W_TSTCC  = Sum_Dev_8Po;
                            Codtrn = W_ADQCTR8;       // 0783=Cr no cta cli
                            Concepto = 'VTA';
                            DesConcepto = 'Dev.8%.Come';
                            Exsr Credito;
                         Endif;
         EndSr;
         Begsr VerCcCome;
           ADQCOME = A02COME;
           chain ( ADQCOME ) IADQCOM;
           If %found(IADQCOM);
             w3ADQCODP = %subst(ADQCODP:1:3);
             Non_Num_Posn = %check (Numbers : w3ADQCODP);
              If Non_Num_Posn > *Zeros;
                 Cc_comis3  = 999;
              Else;
                 Cc_comis3  = %dec(w3ADQCODP: 3:0);
              EndIf;
           else;
                 Cc_comis3  = 999;
           Endif;
         EndSr;
      //* AGREGADO EL 21/07/2025 MEJORA POR ECOMMERCE **************************
         Begsr ValidaTerminal;
            clear esEcommerce;

            if (C_MEMBRESIA = 0);
               monitor;
                  esEcommerce = %scan('E' :A03TERM :1 :1 );
               on-error;
                  esEcommerce = 0;
                  Exsr GuardaLogDeErrores;
               endmon;

               if (esEcommerce > 0); //Es una terminal virtual
                  C_TERMINAL = A03TERM;
                  //Obtener cobros para terminal virtual por comercio
                  Exsr CobrosTerminalVrt;
                  //Si tiene pendiente cobro se obtienen las cuentas para lote
                  if (C_MEMBRESIA <> 0);
                  //Obtiene cuentas donde acreditará cobros por terminal virtual
                     Exsr CuentasControlEcommerce;
                  endif;
               endif;
            endif;
        EndSr;
        Begsr CobrosTerminalVrt;
         chain ( A02COME ) ADQCOBRO;
         if %FOUND(ADQCOBRO);
            if ( ADQCOBRO18 = 'PENDIENTE');
               Exsr ObtieneValorDolar;
               C_MEMBRESIA = %DECH(ADQCOBRO04 * $TasaCompra :8 :2);
               C_STS = ADQCOBRO18;
            endif;
         endif;
        EndSr;
        Begsr CuentasControlEcommerce;
            chain ('EC') ADQECTL;
            if %found(ADQECTL);
               ADQENUM = ADQENUM + 1;
               W_ADQENUM = ADQENUM;
               W_ADQENUM_ch = %char(W_ADQENUM);
               W_ADQECNT1 = ADQECNT1; //MEM UNICA CUENTA CONTABLE
               W_ADQECNT5 = ADQECNT5; //CTA RECHAZO
               W_ADQECCO1 = ADQECCO1; //MEM UNICA C.C
               W_ADQECCO5 = ADQECCO5; //CTA RECHAZO C.C
               // CODIGOS DE TRANSACCIO
               W_ADQECTR1 = ADQECTR1; //MEM UNICA COD TRN 0783=CR
               W_ADQECTR5 = ADQECTR5; //CTA RECHAZO COD TRN 0784=DR
               Update $ADQECTL;
            endif;
        EndSr;
        Begsr CambiaStsCobroMemUnica;
            chain ( A02COME ) ADQCOBRO;
            if %FOUND(ADQCOBRO);
               ADQCOBRO18 = 'PAGADO';
               flagCobro = 'P';
               descripFlag = 'PAGO POSTEADO EN LOTE';
               C_STS = ADQCOBRO18;
               monitor;
                  UPDATE $ADQCOBRO;
               on-error;
                  C_STS = 'ERROR STS';
                  Exsr GuardaLogDeErrores;
               endmon;
            endif;
        EndSr;
        Begsr ObtieneValorDolar;
            Setgt ( 1  : MonedaUSD : *Hival : *Hival ) GLC002;
            Readp GLC002;
            Dow Not %Eof(GLC002);
              If GBCODE = MonedaUSD;
                $TasaCompra = GBBKXR;
                Leave;
              EndIf;
              Readp GLC002;
            Enddo;
        EndSr;
        Begsr HistoricoCobroEcommerce;
            ADQMEMH001 = A02FILE; //ARCHIVO
            monitor;
              ADQMEMH002 = %CHAR(A02ENPR); //ENTIDAD
              ADQMEMH004 = %dec(A02CTDE: 10: 0); //CTA DEPOSITO COMERCIO
            on-error;
              ADQMEMH002 = *blanks;
              ADQMEMH004 = *zeros;
            endmon;
            ADQMEMH003 = A02COME; //CLAVE COMERCIO
            ADQMEMH005 = C_TERMINAL; //TERMINAL VIRTUAL
            ADQMEMH006 = A10LOTE; //CLAVE LOTE
            ADQMEMH007 = A10NUMR; //NUM REFERENCIA
            ADQMEMH008 = 'COMISIÓN MEMEBRESÍA ÚNICA'; //DESCRIPCIÓN
            ADQMEMH009 = A02NACO; //NOMBRE DE COMERCIO
            ADQMEMH010 = C_MEMBRESIA; //COBRO MEMBRESIA UNICA
            ADQMEMH011 = C_STS; //ESTADO COBRO MEMBRESÍA
            ADQMEMH012 = flagCobro; //FLAG COBRO REALIZADO
            //Auditoría
            ADQMEMH013 = %DEC( %DATE() ); //FECHA COBRO
            ADQMEMH014 = %DEC( %TIME() ); //HORA COBRO
            ADQMEMH015 = Usuario; //USUARIO COBRO
            ADQMEMH016 = $TasaCompra; //VALOR DOLAR USD
            ADQMEMH017 = descripFlag; //DESCRIPCIÓN RESULTADO DEL FLAG
            monitor;
               WRITE $ADQMEMH;
            on-error;
              Exsr GuardaLogDeErrores;
            endmon;
        EndSr;
        Begsr GuardaLogDeErrores;
            PROCNME    = PgmDs.PROCNME;
            STSCDE     = PgmDs.STSCDE;
            PRVSTSCDE  = PgmDs.PRVSTSCDE;
            SRCLINNBR  = PgmDs.SRCLINNBR;
            EXCPTSUBR  = PgmDs.EXCPTSUBR;
            NBRPARMS   = PgmDs.NBRPARMS;
            EXCPTTYP   = PgmDs.EXCPTTYP;
            EXCPTNBR   = PgmDs.EXCPTNBR;
            MSGWRKAREA = PgmDs.MSGWRKAREA;
            LIB        = PgmDs.LIB;
            RTVEXCPTDT = PgmDs.RTVEXCPTDT;
            EXCPTID    = PgmDs.EXCPTID;
            DTEJOBENTR = PgmDs.DTEJOBENTR;
            CNTJOBENTR = PgmDs.CNTJOBENTR;
            LASTFILEOP = PgmDs.LASTFILEOP;
            FILESTS    = PgmDs.FILESTS;
            JOBNME     = PgmDs.JOBNME;
            USER       = PgmDs.USER;
            JOBNMBR    = PgmDs.JOBNMBR;
            DTEJOBENT2 = PgmDs.DTEJOBENT2;
            DTEPGMRUN  = PgmDs.DTEPGMRUN;
            TMEPGMRUN  = PgmDs.TMEPGMRUN;
            COMPILEDTE = PgmDs.COMPILEDTE;
            COMPILETME = PgmDs.COMPILETME;
            COMPILELVL = PgmDs.COMPILELVL;
            SRCFILE    = PgmDs.SRCFILE   ;
            SRCLIB     = PgmDs.SRCLIB    ;
            SRCMBR     = PgmDs.SRCMBR    ;
            MODULEPGM  = PgmDs.MODULEPGM ;
            MODULEPROC = PgmDs.MODULEPROC;
            WRITE $ADQERRLOG;
        EndSr;
      //***************************************************************
      /End-free
     c     FecReal       begsr
     C                   call      FECTIM
     c                   parm                    Fecsys
     c                   parm                    Horasys
     c                   endsr




Código nuevo
using Microsoft.AspNetCore.Mvc;
using MS_BAN_56_ProcesamientoTransaccionesPOS.Models.Dtos.Transacciones.GuardarTransacciones;
using MS_BAN_56_ProcesamientoTransaccionesPOS.Services.Transacciones;

namespace MS_BAN_56_ProcesamientoTransaccionesPOS.Controllers;

/// <summary>
/// Controlador para la gestión de transacciones POS
/// </summary>
/// <param name="_transaccionesService"></param>
[Route("api/ProcesamientoTransaccionesPOS/[controller]")]
[ApiController]
public class TransaccionesController(ITransaccionesServices _transaccionesService) : ControllerBase
{
    /// <summary>
    /// Ingreso de los valores de las transacciones POS.
    /// </summary>
    /// <param name="guardarTransaccionesDto"></param>
    /// <returns></returns>
    [HttpPost("GuardarTransacciones")]
    [ProducesResponseType(typeof(RespuestaGuardarTransaccionesDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RespuestaGuardarTransaccionesDto), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GuardarTransacciones([FromBody] GuardarTransaccionesDto guardarTransaccionesDto)
    {

        if (!ModelState.IsValid)
        {
            var dto = new RespuestaGuardarTransaccionesDto
            {
                CodigoError = "400",
                DescripcionError = "Solicitud inválida."
            };

            return BadRequest(dto);
        }

        try
        {
            var respuesta = await _transaccionesService.GuardarTransaccionesAsync(guardarTransaccionesDto);
            
            switch(respuesta.CodigoError)
            {
                case "200":
                    return Ok(respuesta);
                case "400":
                    return BadRequest(respuesta);
                default:
                    var dto = BuildError("500", "Error interno del servidor.");
                    return StatusCode(StatusCodes.Status500InternalServerError, dto);
            }
        }
        catch (Exception ex)
        {
            var dto = new RespuestaGuardarTransaccionesDto
            {
                CodigoError = "400",
                DescripcionError = ex.Message
            };
            return BadRequest(dto);
        }
    }

    /// <summary>
    /// Crea un DTO de respuesta de error con metadatos consistentes.
    /// </summary>
    private static RespuestaGuardarTransaccionesDto BuildError(string code, string message)
        => new()
        {
            CodigoError = code,
            DescripcionError = message
        };
}


using MS_BAN_56_ProcesamientoTransaccionesPOS.Models.Dtos.Transacciones.GuardarTransacciones;

namespace MS_BAN_56_ProcesamientoTransaccionesPOS.Services.Transacciones;

/// <summary>
/// Interface para el servicio de transacciones POS.
/// </summary>
public interface ITransaccionesServices
{
    /// <summary>
    /// Método para guardar las transacciones POS.
    /// </summary>
    /// <param name="guardarTransaccionesDto">Parametro dto, continene la estructura necesaria para la petición.</param>
    /// <returns>Retorna una respuesta Http con un objeto de tipo <![CDATA[>RespuestaGuardarTransaccionesDto]]></returns>
    Task<RespuestaGuardarTransaccionesDto> GuardarTransaccionesAsync(GuardarTransaccionesDto guardarTransaccionesDto);
}


using Connections.Abstractions;
using Microsoft.IdentityModel.Tokens;
using MS_BAN_56_ProcesamientoTransaccionesPOS.Models.AS400.BCAH96DTA;
using MS_BAN_56_ProcesamientoTransaccionesPOS.Models.AS400.BNKPRD01;
using MS_BAN_56_ProcesamientoTransaccionesPOS.Models.Dtos.Transacciones.GuardarTransacciones;
using MS_BAN_56_ProcesamientoTransaccionesPOS.Utils;
using QueryBuilder.Builders;
using QueryBuilder.Enums;

namespace MS_BAN_56_ProcesamientoTransaccionesPOS.Services.Transacciones;

/// <summary>
/// Clase de servicio para el procesamiento de transacciones POS.
/// </summary>
/// <param name="_connection">Inyección de clase IDatabaseConnection.</param>
/// <param name="_contextAccessor">Inyección de clase IHttpContextAccessor.</param>
public class TransaccionesServices(IDatabaseConnection _connection, IHttpContextAccessor _contextAccessor) : ITransaccionesServices
{
    private string perfilTranserver = string.Empty; // Este valor debería ser dinámico o configurado según el contexto real.

    /// <summary>
    /// Represents the response data for saving transactions.
    /// </summary>
    /// <remarks>This field is intended to store an instance of <see cref="RespuestaGuardarTransaccionesDto"/>
    /// that contains the result of a transaction-saving operation. It is protected and can be accessed  or modified by
    /// derived classes.</remarks>
    protected RespuestaGuardarTransaccionesDto _respuestaGuardarTransaccionesDto = new();

    /// <ineheritdoc/>
    public async Task<RespuestaGuardarTransaccionesDto> GuardarTransaccionesAsync(GuardarTransaccionesDto guardarTransaccionesDto)
    {
        await Task.Yield(); // Simula asincronía para cumplir con la firma async.
        //Procesos Previos

        //LLamada a método FecReal, reemplaza llamado a CLLE fecha Real (FECTIM) ICBSUSER/FECTIM
        //  var (error, fecsys, horasys) = FecReal()

        //Llamada a método VerFecha, reemplaza llamado a CLLE VerFecha (DSCDT) BNKPRD01/TAP001
        var (found, yyyyMMdd) = VerFecha();
        if (!found) return BuildError("400", "No se pudo obtener la fecha del sistema.");

        _connection.Open(); //Abrimos la conexión a la base de datos

        //============================Validaciones Previas============================//

        // Normalización de importes: tolera "." o "," y espacios
        var deb = Utilities.ParseMonto(guardarTransaccionesDto.MontoDebitado);
        var cre = Utilities.ParseMonto(guardarTransaccionesDto.MontoAcreditado);

        //Obtenemos perfil transerver de la configuración global
        perfilTranserver = GlobalConnection.GetPerfilTranserver.PerfilTranserver;

        //Validamos, si no hay perfil transerver, retornamos error porque el proceso no puede continuar.
        if (perfilTranserver.IsNullOrEmpty()) return BuildError("400", "No se ha configurado el perfil transerver.");

        //Validamos que al menos uno de los montos sea mayor a 0, no se puede postear ambos en 0.
        if (deb <= 0m && cre <= 0m) return BuildError("400", "No hay importes a postear (ambos montos son 0).");

        //Validamos si existe el comercio en la tabla BCAH96DTA/IADQCOM
        var (existeComercio, codigoError, mensajeComercio) = BuscarComercio(guardarTransaccionesDto.NumeroCuenta, int.Parse(guardarTransaccionesDto.CodigoComercio));
        if (!existeComercio) return BuildError(codigoError, mensajeComercio);

        //Validación de Terminal
        var (existeTerminal, codigoErrorTerminal, mensajeTerminal) = BuscarTerminal(guardarTransaccionesDto.Terminal, int.Parse(guardarTransaccionesDto.CodigoComercio));
        if (!existeTerminal) return BuildError(codigoErrorTerminal, mensajeTerminal);

        //============================Fin Validaciones Previas============================//

        //============================Inicia Proceso Principal============================//

        // 1. Obtenemos el Perfil Transerver del cliente.
        var respuestaPerfil = VerPerfil(perfilTranserver);

        // Si no existe el perfil, retornar error y no continuar con el proceso.
        if (!respuestaPerfil.existePerfil) return BuildError(respuestaPerfil.codigoError, respuestaPerfil.descripcionError);

        // 2. Obtenemos el último lote de la tabla POP801 para el perfil transerver.
        //    Si no existe, se asume 0.
        //    Esto reemplaza la lógica de VerUltLote en RPGLE.
        var (ultimoLote, descripcionUltimoLote) = VerUltLote(perfilTranserver);

        // 3. Llamamos al método NuevoLote con el valor obtenido.
        //    Esto reemplaza la lógica de NuevoLote en RPGLE.
        var (numeroLote, existeLote) = NuevoLote(perfilTranserver, "usuario", ultimoLote, ultimoLote);
        if (!existeLote) return BuildError("400", "No se pudo crear un nuevo lote. " + descripcionUltimoLote);

        //Validar 
        int secuencia = 0;

        //Validación de naturaleza contable
        switch (guardarTransaccionesDto.NaturalezaContable)
        {
            case "C":
                //Proceso de crédito
                secuencia += 1;
                InsertPop802(
                    perfil: perfilTranserver,
                    lote: numeroLote,
                    seq: secuencia,
                    fechaYyyyMmDd: Convert.ToInt32(yyyyMMdd),
                    cuenta: guardarTransaccionesDto.NumeroCuenta,      // TSTACT: cuenta objetivo (cliente/comercio)
                    centroCosto: 0,                // TSWSCC: si requieres C.C., cámbialo aquí
                    codTrn: "0783",                // 0783 = Crédito (convención del core)
                    monto: cre,
                    al1: guardarTransaccionesDto.NombreComercio,       // leyenda 1
                    al2: $"{guardarTransaccionesDto.CodigoComercio}-{guardarTransaccionesDto.Terminal}", // leyenda 2
                    al3: $"&{EtiquetaConcepto(guardarTransaccionesDto.NaturalezaContable)}&{guardarTransaccionesDto.IdTransaccionUnico}&Cr Tot." // leyenda 3
                );
                break;
            case "D":
                //Proceso de débito
                secuencia += 1;
                InsertPop802(
                    perfil: perfilTranserver,
                    lote: numeroLote,
                    seq: secuencia,
                    fechaYyyyMmDd: Convert.ToInt32(yyyyMMdd),
                    cuenta: guardarTransaccionesDto.NumeroCuenta,
                    centroCosto: 0,
                    codTrn: "0784",                // 0784 = Débito (convención del core)
                    monto: deb,
                    al1: guardarTransaccionesDto.NombreComercio,
                    al2: $"{guardarTransaccionesDto.CodigoComercio}-{guardarTransaccionesDto.Terminal}",
                    al3: $"&{EtiquetaConcepto(guardarTransaccionesDto.NaturalezaContable)}&{guardarTransaccionesDto.IdTransaccionUnico}&Db Tot."
                );
                break;
            default:
                return BuildError("00001", "Naturaleza contable inválida.");
        }

        return BuildError(code: "200", message: "Transacción procesada correctamente.");
    }

    // ============================ Utilidades ============================

    /// <summary>
    /// Equivalente a: 
    /// <c>CALL FECTIM PARM(FAAAAMMDD HORA)</c>.
    /// </summary>
    /// <returns>
    /// (respuesta: true/false, fecsys: "yyyyMMdd" (8), horasys: "HHmmss" (7))
    /// </returns>
    public (bool respuesta, string fecsys, string horasys) FecReal()
    {
        // Variables de salida: simulan los PARM de CLLE.
        string fecsys = string.Empty;   // &FAAAAMMDD (8)
        string horasys = string.Empty;  // &HORA      (7)

        try
        {
            // ================== SQL generado ==================
            // SELECT
            //   CURRENT_DATE AS FECHA,
            //   CURRENT_TIME AS HORA
            // FROM SYSIBM.SYSDUMMY1
            // ==================================================
            var query = QueryBuilder.Core.QueryBuilder
                .From("SYSDUMMY1", "SYSIBM")
                .Select(
                    "CURRENT_DATE AS FECHA",
                    "CURRENT_TIME AS HORA"
                )
                .FetchNext(1)
                .Build();

            using var cmd = _connection.GetDbCommand(_contextAccessor.HttpContext!);
            cmd.CommandText = query.Sql;

            using var rd = cmd.ExecuteReader();
            if (!rd.Read())
                return (false, fecsys, horasys);

            // Lectura directa por índice para máximo rendimiento
            fecsys = rd.GetString(0).Replace("-", "");                 // "yyyyMMdd" (8)
            horasys = rd.GetString(1).PadRight(7).Replace(".", "");     // "HHmmss" -> ajustado a LEN(7)

            return (true, fecsys, horasys);
        }
        catch
        {
            // Si hay error, mantenemos contrato similar al PGM (bandera false)
            return (false, fecsys, horasys);
        }
    }

    /// <summary>
    /// Lee DSCDT desde BNKPRD01.TAP001 (DSBK=001) y retorna:
    /// - el valor bruto DSCDT (CYYMMDD)
    /// - la fecha formateada YYYYMMDD
    /// </summary>
    /// <returns>seObtuvoFecha, dscdtCyyMmDd, yyyyMMdd</returns>
    private (bool seObtuvoFecha,string yyyyMMdd) VerFecha()
    {
        // Valores de salida predeterminados para conservar contrato estable.
        var dscdt = 0;            // valor crudo CYYMMDD
        var yyyyMMdd = string.Empty;

        try
        {
            // SELECT DSCDT FROM BNKPRD01.TAP001 WHERE DSBK = 1 FETCH FIRST 1 ROW ONLY
            // - Se usa DTO para habilitar lambdas tipadas y evitar cadenas mágicas.
            var query = QueryBuilder.Core.QueryBuilder
                .From("TAP001", "BNKPRD01")
                .Select("DSCDT")             // solo la columna necesaria
                .Where<Tap001>(x => x.DSBK == 1)          // DSBK = 001 en RPGLE
                .FetchNext(1)                              // equivalente a CHAIN + %FOUND
                .Build();

            using var cmd = _connection.GetDbCommand(_contextAccessor.HttpContext!);
            cmd.CommandText = query.Sql;

            using var rd = cmd.ExecuteReader();
            if (!rd.Read())
                return (false, yyyyMMdd);

            // Lectura directa: índice 0 porque solo seleccionamos DSCDT.
            dscdt = rd.GetInt32(0);

            // Conversión de CYYMMDD → YYYYMMDD para uso homogéneo en .NET/SQL.
            yyyyMMdd = ConvertCyyMmDdToYyyyMmDd(dscdt);

            return (true, yyyyMMdd);
        }
        catch
        {             // En caso de error, retornamos valores predeterminados.
            return (false, yyyyMMdd);
        }
    }

    /// <summary>
    /// Convierte un entero en formato IBM i CYYMMDD (p. ej. 1240912) a "YYYYMMDD".
    /// </summary>
    /// <remarks>
    /// - C: siglo relativo a 1900 (0=>1900, 1=>2000, etc.).  
    /// - YY: año dentro del siglo.  
    /// - MM: mes, DD: día.
    /// </remarks>
    private static string ConvertCyyMmDdToYyyyMmDd(int cyymmdd)
    {
        // Separación de C, YY, MM, DD usando división/módulo para evitar parseos de string.
        var c = cyymmdd / 1000000;                 // dígito del siglo
        var yy = (cyymmdd / 10000) % 100;            // dos dígitos de año
        var mm = (cyymmdd / 100) % 100;            // mes
        var dd = cyymmdd % 100;            // día

        // Año absoluto: 1900 + (C * 100) + YY. Para C=1 => 2000+YY.
        var yyyy = 1900 + (c * 100) + yy;

        // Composición sin separadores para uso en sistemas que requieren 8 caracteres.
        return $"{yyyy:0000}{mm:00}{dd:00}";
    }


    /// <summary>
    /// Método de validación de existencia de comercio en tabla BCAH96DTA/IADQCOM.
    /// </summary>
    /// <param name="cuentaRecibida">Número de cuenta recibido en la petición.</param>
    /// <param name="codigoComercioRecibido">Código de Comercio recibido en la petición.</param>
    /// <returns>Retorna un tupla
    /// /// (existeComercio: true/false, codigoError: "000001" , mensajeComercio: "Descripcioón del error")
    /// </returns>
    private (bool existeComercio, string codigoError, string mensajeComercio) BuscarComercio(string cuentaRecibida, int codigoComercioRecibido)
    {
        try
        {
            // Construimos consulta SQL con QueryBuilder para verificar existencia de perfil
            var buscarComercio = QueryBuilder.Core.QueryBuilder
                .From("IADQCOM", "BCAH96DTA")
                .Select("*")  // Solo necesitamos validar existencia
                .Where<AdqCom>(x => x.ADQCOME == codigoComercioRecibido)
                .Where<AdqCom>(x => x.ADQCTDE == cuentaRecibida) // Filtro dinámico por perfil
                .FetchNext(1)                // Solo necesitamos un registro
                .OrderBy("ADQCOME", QueryBuilder.Enums.SortDirection.Asc)
                .Build();

            using var command = _connection.GetDbCommand(buscarComercio, _contextAccessor.HttpContext!);

            using var reader = command.ExecuteReader();
            if (reader.Read())
            {
                return (true, "00001", "Existe Comercio."); // Coemrcio existe
            }
            return (false, "00002", "No existe Comercio."); // Comercio no existe
        }
        catch (Exception ex)
        {
            // Manejo de errores en la consulta
            return (false, "0003", ex.Message); // Indica error al consultar comercio
        }
    }

    /// <summary>
    /// Método de validación de existencia de terminal en tabla BCAH96DTA/ADQ03TER.
    /// </summary>
    /// <param name="terminalRecibida">Número de terminal Recibida</param>
    /// <param name="codigoComercioRecibido">Código Comercio Recibido.</param>
    /// <returns></returns>
    private (bool existeTerminal, string codigoError, string mensajeTerminal) BuscarTerminal(string terminalRecibida, int codigoComercioRecibido)
    {
        try
        {
            // Construimos consulta SQL con QueryBuilder para verificar existencia de perfil
            var buscarComercio = QueryBuilder.Core.QueryBuilder
                .From("ADQ03TER", "BCAH96DTA")
                .Select("*")  // Solo necesitamos validar existencia
                .Where<Adq03Ter>(x => x.A03COME == codigoComercioRecibido)
                .Where<Adq03Ter>(x => x.A03TERM == terminalRecibida)
                .OrderBy(("A03TERM", SortDirection.Asc), ("A03TERM", SortDirection.Asc))
                .Build();

            using var command = _connection.GetDbCommand(buscarComercio, _contextAccessor.HttpContext!);

            using var reader = command.ExecuteReader();
            if (reader.Read())
            {
                return (true, "00001", "Existe terminal."); // Terminal existe
            }
            return (false, "00002", "No existe terminal."); // Terminal no existe
        }
        catch (Exception ex)
        {
            // Manejo de errores en la consulta
            return (false, "0003", ex.Message); // Indica error al consultar comercio
        }
    }



    /// <summary>
    /// Verifica si existe un perfil en la tabla CFP801 y ejecuta la lógica correspondiente.
    /// </summary>
    /// <param name="perfil">Clave de perfil (CFTSKY en RPGLE).</param>
    /// <returns>Tupla (bool, string,  string), true o false y descripción si existe o no el perfil</returns>
    private (bool existePerfil, string codigoError, string descripcionError) VerPerfil(string perfil)
    {
        try
        {
            // Construimos consulta SQL con QueryBuilder para verificar existencia de perfil
            var verPerfilSql = QueryBuilder.Core.QueryBuilder
                .From("CFP801", "BCAH96DTA")
                .Select("CFTSBK", "CFTSKY")  // Solo necesitamos validar existencia
                .Where<Cfp801>(x => x.CFTSBK == 001)       // Condición fija
                .Where<Cfp801>(x => x.CFTSKY == perfil) // Filtro dinámico por perfil
                .FetchNext(1)                // Solo necesitamos un registro
                .Build();

            using var command = _connection.GetDbCommand(verPerfilSql, _contextAccessor.HttpContext!);

            using var reader = command.ExecuteReader();
            if (reader.Read())
            {
                return (true, "00001", "Existe Perfil Transerver."); // Perfil existe
            }
            return (false, "00002", "No existe Perfil Transerver."); // Perfil no existe
        }
        catch (Exception ex)
        {
            // Manejo de errores en la consulta
            return (false, "0003", "Error general: " + ex.Message); // Indica error al consultar perfil
        }
    }

    /// <summary>
    /// Obtiene el último valor de FTSBT para un perfil dado (equivalente al VerUltlote en RPGLE).
    /// </summary>
    /// <param name="perfil">Clave de perfil que corresponde a FTTSKY.</param>
    /// <returns>El último valor de FTSBT encontrado o 0 si no existe.</returns>
    private (int ultimoLote, string descripcionUltimoLote) VerUltLote(string perfil)
    {
        // Variable resultado (equivalente a wFTSBT en RPGLE)
        int ultimoFTSBT = 0;

        try
        {

            // Construimos el query con QueryBuilder
            var ultimoLoteQuery = QueryBuilder.Core.QueryBuilder
                .From("POP801", "BCAH96DTA")   // Tabla POP801 en librería AS400
                .Select("FTSBT")               // Campo que queremos traer
                .WhereRaw("FTTSBK = 001")         // Condición fija de RPGLE
                .Where<Pop801>(x => x.FTTSKY == perfil) // Filtro dinámico por PERFIL
                .OrderBy("FTSBT DESC")         // Simula leer hasta el último FTSBT
                .FetchNext(1)                  // Solo el último
                .Build();

            using var command = _connection.GetDbCommand(ultimoLoteQuery, _contextAccessor.HttpContext!);

            using var reader = command.ExecuteReader();
            if (reader.Read())
            {
                ultimoFTSBT = reader.GetInt32(0);
            }
            return (ultimoFTSBT, ultimoFTSBT > 0 ? "Se encontro Último." : "No se encontro último lote.");
        }
        catch (Exception ex)
        {
            return (0, ex.Message); // Retorna 0 en caso de error
        }
    }

    /// <summary>
    /// Inserta un nuevo lote en <c>BNKPRD01.POP801</c>.
    /// </summary>
    /// <param name="perfil">Valor para FTTSKY.</param>
    /// <param name="usuario">Valor para FTTSOR.</param>
    /// <param name="dsdt">Fecha operativa (CYYMMDD) para FTTSDT.</param>
    /// <param name="ultimoFtsbt">Último FTSBT existente (para calcular el siguiente).</param>
    /// <returns>El número de lote generado (FTSBT) y si se persistió correctamente.</returns>
    private (int numeroLote, bool existeLote) NuevoLote(string perfil, string usuario, int dsdt, int ultimoFtsbt)
    {
        // ► En RPG: wFTSBT = wFTSBT + 1; FTTSBK = 001; FTTSKY = PERFIL; FTSBT = wFTSBT; FTSST = 02; FTTSOR = Usuario; FTTSDT = DSDT; write Pop8011

        var siguienteFtsbt = ultimoFtsbt + 1; // número de lote que se insertará

        try
        {
            // IntoColumns define el orden de columnas; Row especifica los valores respetando ese orden.
            var insertNuevoLote = new InsertQueryBuilder("POP801", "BNKPRD01")
                .IntoColumns("FTTSBK", "FTTSKY", "FTTSBT", "FTTSST", "FTTSOR", "FTTSDT")
                .Row([1, perfil, siguienteFtsbt, 2, usuario, dsdt])
                .Build();

            using var cmd = _connection.GetDbCommand(insertNuevoLote, _contextAccessor.HttpContext!);

            var affected = cmd.ExecuteNonQuery(); // write Pop8011

            return (siguienteFtsbt, affected > 0);
        }
        catch
        {
            return (0, false); // En caso de error, retornamos 0 y false.}
        }
    }

    /// <summary>
    /// Inserta una fila en POP802 (detalle de posteo) con campos esenciales.
    /// </summary>
    private void InsertPop802(
        string perfil,
        int lote,
        int seq,
        int fechaYyyyMmDd,
        string cuenta,
        int centroCosto,
        string codTrn,
        decimal monto,
        string al1,
        string al2,
        string al3)
    {
        // Nota funcional: POP802 requiere varias columnas obligatorias del core.
        // Aquí posteamos lo esencial (override, fecha, cuenta, tcode, monto y leyendas).
        var pop802Sql = new InsertQueryBuilder("POP802", "BNKPRD01")
            .IntoColumns(
                "TSBK",    // Bank
                "TSTSKY",  // Perfil
                "TSBTCH",  // Lote
                "TSWSEQ",  // Secuencia
                "TSTOVR",  // Override
                "TSTTDT",  // Fecha efectiva (YYYYMMDD)
                "TSTACT",  // Cuenta
                "TSWSCC",  // Centro de costo
                "TSWTCD",  // Código de transacción
                "TSTCC",   // Monto
                "TSTAL1",  // Leyenda 1
                "TSTAL2",  // Leyenda 2
                "TSTAL3"   // Leyenda 3
            )
            .Row([
                1,
                perfil,
                lote,
                seq,
                "S",
                fechaYyyyMmDd,
                cuenta,
                centroCosto,
                codTrn,
                monto,
                Trunc(al1, 30),
                Trunc(al2, 30),
                Trunc(al3, 30)
            ])
            .Build();

        using var cmd = _connection.GetDbCommand(pop802Sql, _contextAccessor.HttpContext!);

        var aff = cmd.ExecuteNonQuery();

        if (aff <= 0) throw new InvalidOperationException("No se pudo insertar el detalle POP802.");
    }

    /// <summary>
    /// Método auxiliar para truncar cadenas a una longitud máxima.
    /// </summary>
    /// <param name="s"></param>
    /// <param name="max"></param>
    /// <returns></returns>
    private static string Trunc(string? s, int max)
    {
        if (string.IsNullOrEmpty(s))
            return string.Empty;
        if (s.Length <= max)
            return s;
        return s[..max];
    }

    /// <summary>Convierte "C"/"D" a etiqueta corta funcional.</summary>
    private static string EtiquetaConcepto(string nat) => (nat ?? "C").Equals("D", StringComparison.InvariantCultureIgnoreCase) ? "DB" : "CR";

    /// <summary>
    /// Crea un DTO de respuesta de error con metadatos consistentes.
    /// </summary>
    private static RespuestaGuardarTransaccionesDto BuildError(string code, string message)
        => new()
        {
            CodigoError = code,
            DescripcionError = message
        };
}


Me indicas si requieres otro segmento de código.
