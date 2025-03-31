PGM PARM(
  &AGTCD +
  &TRNTYPCD +
  &CNFNM +
  &REGNSD +
  &BRNCHSD +
  &STCD +
  &CTRYCD +
  &USRNAME +
  &TRMINAL +
  &AGTDATE +
  &AGTTIME +
  &REQID +
  &CHNL +
  &SESSID +
  &CLNTIP +
  &USRID +
  &PROVIDR +
  &ORGZTN +
  &TRMNLHD +
  &TMSTMPHD +
  &HDR_RSPID +
  &HDR_TMSTMP +
  &HDR_PRTIME +
  &HDR_STSCD +
  &HDR_MSG +
  &DAT_OPCODE +
  &DAT_PRCMSG +
  &DAT_ERRPAR +
  &DAT_TRSSTS +
  &DAT_TRSDT +
  &DAT_PRCSDT +
  &DAT_PRCSTM +
  &DDT_SALEDT +
  &DDT_SALETIM +
  &DDT_SRVC_CD +
  &DDT_PAYTYP +
  &DDT_ORGCTY +
  &DDT_ORGCRY +
  &DDT_DSTCTY +
  &DDT_DSTCRY +
  &DDT_ORGAMT +
  &DDT_DSTAMT +
  &DDT_EXRATE +
  &DDT_MRCCRY +
  &DDT_MRCAMT +
  &DDT_SAGTCD +
  &DDT_RACCTYP +
  &DDT_RACCNM +
  &DDT_RAGTCD +
  &DDT_RAGTREG +
  &DDT_RAGTBRN +
  &SND_FNAME +
  &SND_MNAME +
  &SND_LNAME +
  &SND_MOMNM +
  &SND_ADDR +
  &SND_CITY +
  &SND_STCD +
  &SND_CTRY +
  &SND_ZIP +
  &SND_PHONE +
  &REC_FNAME +
  &REC_MNAME +
  &REC_LNAME +
  &REC_MOMNM +
  &REC_IDTYP +
  &REC_IDNM +
  &RFC_FNAME +
  &RFC_MNAME +
  &RFC_LNAME +
  &RFC_MOMNM +
  &REC_ADDR +
  &REC_CITY +
  &REC_STCD +
  &REC_CTRY +
  &REC_ZIP +
  &REC_PHONE +
  &RID_TYPCD +
  &RID_ISSCD +
  &RID_ISSST +
  &RID_ISSCT +
  &RID_IDNM +
  &RID_EXPDT +
  &SID_TYPCD +
  &SID_ISSCD +
  &SID_ISSST +
  &SID_ISSCT +
  &SID_IDNM +
  &SID_EXPDT )

DCL VAR(&AGTCD) TYPE(*CHAR) LEN(50)
DCL VAR(&TRNTYPCD) TYPE(*CHAR) LEN(50)
DCL VAR(&CNFNM) TYPE(*CHAR) LEN(50)
DCL VAR(&REGNSD) TYPE(*CHAR) LEN(50)
DCL VAR(&BRNCHSD) TYPE(*CHAR) LEN(50)
DCL VAR(&STCD) TYPE(*CHAR) LEN(50)
DCL VAR(&CTRYCD) TYPE(*CHAR) LEN(50)
DCL VAR(&USRNAME) TYPE(*CHAR) LEN(50)
DCL VAR(&TRMINAL) TYPE(*CHAR) LEN(50)
DCL VAR(&AGTDATE) TYPE(*CHAR) LEN(50)
DCL VAR(&AGTTIME) TYPE(*CHAR) LEN(50)
DCL VAR(&REQID) TYPE(*CHAR) LEN(50)
DCL VAR(&CHNL) TYPE(*CHAR) LEN(50)
DCL VAR(&SESSID) TYPE(*CHAR) LEN(50)
DCL VAR(&CLNTIP) TYPE(*CHAR) LEN(50)
DCL VAR(&USRID) TYPE(*CHAR) LEN(50)
DCL VAR(&PROVIDR) TYPE(*CHAR) LEN(50)
DCL VAR(&ORGZTN) TYPE(*CHAR) LEN(50)
DCL VAR(&TRMNLHD) TYPE(*CHAR) LEN(50)
DCL VAR(&TMSTMPHD) TYPE(*CHAR) LEN(50)

DCL VAR(&HDR_RSPID) TYPE(*CHAR) LEN(100)
DCL VAR(&HDR_TMSTMP) TYPE(*CHAR) LEN(100)
DCL VAR(&HDR_PRTIME) TYPE(*CHAR) LEN(100)
DCL VAR(&HDR_STSCD) TYPE(*CHAR) LEN(100)
DCL VAR(&HDR_MSG) TYPE(*CHAR) LEN(100)
DCL VAR(&DAT_OPCODE) TYPE(*CHAR) LEN(100)
DCL VAR(&DAT_PRCMSG) TYPE(*CHAR) LEN(100)
DCL VAR(&DAT_ERRPAR) TYPE(*CHAR) LEN(100)
DCL VAR(&DAT_TRSSTS) TYPE(*CHAR) LEN(100)
DCL VAR(&DAT_TRSDT) TYPE(*CHAR) LEN(100)
DCL VAR(&DAT_PRCSDT) TYPE(*CHAR) LEN(100)
DCL VAR(&DAT_PRCSTM) TYPE(*CHAR) LEN(100)
DCL VAR(&DDT_SALEDT) TYPE(*CHAR) LEN(100)
DCL VAR(&DDT_SALETIM) TYPE(*CHAR) LEN(100)
DCL VAR(&DDT_SRVC_CD) TYPE(*CHAR) LEN(100)
DCL VAR(&DDT_PAYTYP) TYPE(*CHAR) LEN(100)
DCL VAR(&DDT_ORGCTY) TYPE(*CHAR) LEN(100)
DCL VAR(&DDT_ORGCRY) TYPE(*CHAR) LEN(100)
DCL VAR(&DDT_DSTCTY) TYPE(*CHAR) LEN(100)
DCL VAR(&DDT_DSTCRY) TYPE(*CHAR) LEN(100)
DCL VAR(&DDT_ORGAMT) TYPE(*CHAR) LEN(100)
DCL VAR(&DDT_DSTAMT) TYPE(*CHAR) LEN(100)
DCL VAR(&DDT_EXRATE) TYPE(*CHAR) LEN(100)
DCL VAR(&DDT_MRCCRY) TYPE(*CHAR) LEN(100)
DCL VAR(&DDT_MRCAMT) TYPE(*CHAR) LEN(100)
DCL VAR(&DDT_SAGTCD) TYPE(*CHAR) LEN(100)
DCL VAR(&DDT_RACCTYP) TYPE(*CHAR) LEN(100)
DCL VAR(&DDT_RACCNM) TYPE(*CHAR) LEN(100)
DCL VAR(&DDT_RAGTCD) TYPE(*CHAR) LEN(100)
DCL VAR(&DDT_RAGTREG) TYPE(*CHAR) LEN(100)
DCL VAR(&DDT_RAGTBRN) TYPE(*CHAR) LEN(100)
DCL VAR(&SND_FNAME) TYPE(*CHAR) LEN(100)
DCL VAR(&SND_MNAME) TYPE(*CHAR) LEN(100)
DCL VAR(&SND_LNAME) TYPE(*CHAR) LEN(100)
DCL VAR(&SND_MOMNM) TYPE(*CHAR) LEN(100)
DCL VAR(&SND_ADDR) TYPE(*CHAR) LEN(100)
DCL VAR(&SND_CITY) TYPE(*CHAR) LEN(100)
DCL VAR(&SND_STCD) TYPE(*CHAR) LEN(100)
DCL VAR(&SND_CTRY) TYPE(*CHAR) LEN(100)
DCL VAR(&SND_ZIP) TYPE(*CHAR) LEN(100)
DCL VAR(&SND_PHONE) TYPE(*CHAR) LEN(100)
DCL VAR(&REC_FNAME) TYPE(*CHAR) LEN(100)
DCL VAR(&REC_MNAME) TYPE(*CHAR) LEN(100)
DCL VAR(&REC_LNAME) TYPE(*CHAR) LEN(100)
DCL VAR(&REC_MOMNM) TYPE(*CHAR) LEN(100)
DCL VAR(&REC_IDTYP) TYPE(*CHAR) LEN(100)
DCL VAR(&REC_IDNM) TYPE(*CHAR) LEN(100)
DCL VAR(&RFC_FNAME) TYPE(*CHAR) LEN(100)
DCL VAR(&RFC_MNAME) TYPE(*CHAR) LEN(100)
DCL VAR(&RFC_LNAME) TYPE(*CHAR) LEN(100)
DCL VAR(&RFC_MOMNM) TYPE(*CHAR) LEN(100)
DCL VAR(&REC_ADDR) TYPE(*CHAR) LEN(100)
DCL VAR(&REC_CITY) TYPE(*CHAR) LEN(100)
DCL VAR(&REC_STCD) TYPE(*CHAR) LEN(100)
DCL VAR(&REC_CTRY) TYPE(*CHAR) LEN(100)
DCL VAR(&REC_ZIP) TYPE(*CHAR) LEN(100)
DCL VAR(&REC_PHONE) TYPE(*CHAR) LEN(100)
DCL VAR(&RID_TYPCD) TYPE(*CHAR) LEN(100)
DCL VAR(&RID_ISSCD) TYPE(*CHAR) LEN(100)
DCL VAR(&RID_ISSST) TYPE(*CHAR) LEN(100)
DCL VAR(&RID_ISSCT) TYPE(*CHAR) LEN(100)
DCL VAR(&RID_IDNM) TYPE(*CHAR) LEN(100)
DCL VAR(&RID_EXPDT) TYPE(*CHAR) LEN(100)
DCL VAR(&SID_TYPCD) TYPE(*CHAR) LEN(100)
DCL VAR(&SID_ISSCD) TYPE(*CHAR) LEN(100)
DCL VAR(&SID_ISSST) TYPE(*CHAR) LEN(100)
DCL VAR(&SID_ISSCT) TYPE(*CHAR) LEN(100)
DCL VAR(&SID_IDNM) TYPE(*CHAR) LEN(100)
DCL VAR(&SID_EXPDT) TYPE(*CHAR) LEN(100)

CALL PGM(BTS01POST) PARM(
  &AGTCD +
  &TRNTYPCD +
  &CNFNM +
  &REGNSD +
  &BRNCHSD +
  &STCD +
  &CTRYCD +
  &USRNAME +
  &TRMINAL +
  &AGTDATE +
  &AGTTIME +
  &REQID +
  &CHNL +
  &SESSID +
  &CLNTIP +
  &USRID +
  &PROVIDR +
  &ORGZTN +
  &TRMNLHD +
  &TMSTMPHD +
  &HDR_RSPID +
  &HDR_TMSTMP +
  &HDR_PRTIME +
  &HDR_STSCD +
  &HDR_MSG +
  &DAT_OPCODE +
  &DAT_PRCMSG +
  &DAT_ERRPAR +
  &DAT_TRSSTS +
  &DAT_TRSDT +
  &DAT_PRCSDT +
  &DAT_PRCSTM +
  &DDT_SALEDT +
  &DDT_SALETIM +
  &DDT_SRVC_CD +
  &DDT_PAYTYP +
  &DDT_ORGCTY +
  &DDT_ORGCRY +
  &DDT_DSTCTY +
  &DDT_DSTCRY +
  &DDT_ORGAMT +
  &DDT_DSTAMT +
  &DDT_EXRATE +
  &DDT_MRCCRY +
  &DDT_MRCAMT +
  &DDT_SAGTCD +
  &DDT_RACCTYP +
  &DDT_RACCNM +
  &DDT_RAGTCD +
  &DDT_RAGTREG +
  &DDT_RAGTBRN +
  &SND_FNAME +
  &SND_MNAME +
  &SND_LNAME +
  &SND_MOMNM +
  &SND_ADDR +
  &SND_CITY +
  &SND_STCD +
  &SND_CTRY +
  &SND_ZIP +
  &SND_PHONE +
  &REC_FNAME +
  &REC_MNAME +
  &REC_LNAME +
  &REC_MOMNM +
  &REC_IDTYP +
  &REC_IDNM +
  &RFC_FNAME +
  &RFC_MNAME +
  &RFC_LNAME +
  &RFC_MOMNM +
  &REC_ADDR +
  &REC_CITY +
  &REC_STCD +
  &REC_CTRY +
  &REC_ZIP +
  &REC_PHONE +
  &RID_TYPCD +
  &RID_ISSCD +
  &RID_ISSST +
  &RID_ISSCT +
  &RID_IDNM +
  &RID_EXPDT +
  &SID_TYPCD +
  &SID_ISSCD +
  &SID_ISSST +
  &SID_ISSCT +
  &SID_IDNM +
  &SID_EXPDT )

ENDPGM
