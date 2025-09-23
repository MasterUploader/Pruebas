using (var cmd = _connection.GetDbCommand(_contextAccessor.HttpContext!))
{
    cmd.CommandText = "CALL QSYS.QCMDEXC('CHGLIBL LIBL(QTEMP BCAH96DTA BNKPRD01 QGPL)', 0000000041.00000)";
    cmd.ExecuteNonQuery();
}
