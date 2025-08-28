Tengo esto actualmente, agrega el fragmento de JSON que falta, o almenos que sea en otro archivo

{
    "code-for-ibmi.connections": [
        {
            "name": "Desarrollo",
            "host": "DVHNDEV",
            "port": 22,
            "username": "TBBANEGA1",
            "readyTimeout": 20000
        },
        {
            "name": "UAT",
            "host": "DVHNUAT",
            "port": 446,
            "username": "TBBANEGA1",
            "readyTimeout": 20000
        }
    ],
    "code-for-ibmi.connectionSettings": [
        {
            "name": "Desarrollo",
            "host": "",
            "objectFilters": [
                {
                    "name": "BCAH96",
                    "filterType": "simple",
                    "library": "BCAH96",
                    "object": "QRPGLESRC, QCLSRC, QDDSSRC, ADQUIRENCI",
                    "types": [
                        "*SRCPF"
                    ],
                    "member": "*",
                    "memberType": "*",
                    "protected": false,
                    "buttons": "SAVE"
                },
                {
                    "name": "BTS",
                    "filterType": "simple",
                    "library": "BTS",
                    "object": "QRPGLESRC, QCLSRC, QDDSSRC",
                    "types": [
                        "*SRCPF"
                    ],
                    "member": "*",
                    "memberType": "*",
                    "protected": false,
                    "buttons": "SAVE"
                }
            ],
            "libraryList": [
                "CYBERDTA",
                "CYBERPGM",
                "GX",
                "BCAH96DTA",
                "BCAH96",
                "BNKPRD01",
                "HOMEPGM",
                "HOMEDTA",
                "CMBWEBDTA",
                "CMBWEBPGM",
                "QGPL",
                "EVERTECDTA",
                "EVERTECPGM",
                "SEASMG3DTA",
                "SEASMG3PRG",
                "QSYS2"
            ],
            "autoClearTempData": false,
            "customVariables": [],
            "connectionProfiles": [],
            "commandProfiles": [],
            "ifsShortcuts": [
                "/home/TBBANEGA1",
                "/TMP/REMESAS/BTS"
            ],
            "autoSortIFSShortcuts": false,
            "homeDirectory": "/home/TBBANEGA1",
            "tempLibrary": "ILEDITOR",
            "tempDir": "/tmp",
            "currentLibrary": "QGPL",
            "sourceFileCCSID": "*FILE",
            "autoConvertIFSccsid": false,
            "hideCompileErrors": [],
            "enableSourceDates": false,
            "sourceDateGutter": false,
            "encodingFor5250": "default",
            "terminalFor5250": "default",
            "setDeviceNameFor5250": false,
            "connectringStringFor5250": "+uninhibited localhost",
            "autoSaveBeforeAction": false,
            "showDescInLibList": false,
            "debugPort": "8005",
            "debugSepPort": "8008",
            "debugUpdateProductionFiles": false,
            "debugEnableDebugTracing": false,
            "readOnlyMode": false,
            "quickConnect": true,
            "defaultDeploymentMethod": "",
            "protectedPaths": [],
            "showHiddenFiles": true,
            "lastDownloadLocation": "C:\\Users\\93421",
            "sourceASP": null,
            "buttons": "save"
        },
        {
            "name": "UAT",
            "host": "",
            "objectFilters": [],
            "libraryList": [],
            "autoClearTempData": false,
            "customVariables": [],
            "connectionProfiles": [],
            "commandProfiles": [],
            "ifsShortcuts": [],
            "autoSortIFSShortcuts": false,
            "homeDirectory": ".",
            "tempLibrary": "ILEDITOR",
            "tempDir": "/tmp",
            "currentLibrary": "",
            "sourceFileCCSID": "*FILE",
            "autoConvertIFSccsid": false,
            "hideCompileErrors": [],
            "enableSourceDates": false,
            "sourceDateGutter": false,
            "encodingFor5250": "default",
            "terminalFor5250": "default",
            "setDeviceNameFor5250": false,
            "connectringStringFor5250": "+uninhibited localhost",
            "autoSaveBeforeAction": false,
            "showDescInLibList": false,
            "debugPort": "8005",
            "debugSepPort": "8008",
            "debugUpdateProductionFiles": false,
            "debugEnableDebugTracing": false,
            "readOnlyMode": false,
            "quickConnect": true,
            "defaultDeploymentMethod": "",
            "protectedPaths": [],
            "showHiddenFiles": true,
            "lastDownloadLocation": "C:\\Users\\93421"
        }
    ],
    "vscode-db2i.alwaysStartSQLJob": "new"
}
