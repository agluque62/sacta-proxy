@cls
@echo ********************************************************
@echo *******    ESTADO DATOS REPLICACIÓN NODO LOCAL   *******
@echo ********************************************************
@echo off
SET estado=0
"C:\Program Files\MySQL\MySQL Server 5.6\bin\mysql" -uroot -pcd40 -f -n < ".\data_replication_slave_status.sql" > estado_data_slave.txt
"C:\Program Files\MySQL\MySQL Server 5.6\bin\mysql" -uroot -pcd40 -f -n < ".\data_replication_master_status.sql" > estado_data_master.txt
rem findstr /L "Master_Log_File" estado_data.txt

rem if "%ERRORLEVEL%"=="0" goto onLocal
rem goto remoto

rem :onLocal
rem SET estado=2

rem pause

