Corrije el SelectQueryBuilder porque los order lo genera así

si coloco .Select("COUNT(*)")
SELECT COUNT(*) AS COUNT_* 
Y Deberia ser

y si lo coloco asi .Select("COUNT(*) AS CNT")
SELECT COUNT(*) as CNT AS COUNT_* 

y lo correcto seria SELECT COUNT(*) o SELECT COUNT(*) AS CNT

