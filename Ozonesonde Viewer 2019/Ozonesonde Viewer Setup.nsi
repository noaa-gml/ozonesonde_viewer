#installer generation script

#include the modern user interface plugin
!include MUI2.nsh

##########################################################################################▼

!define EXEC_PATH "bin\release"
!define EXEC_NAME "Ozonesonde Viewer 2019.exe"

!getdllversion "${EXEC_PATH}\${EXEC_NAME}" expv_
!echo "Explorer.exe version is ${expv_1}.${expv_2}.${expv_3}.${expv_4}"

!define PROG_VERSION "${expv_1}.${expv_2}.${expv_3}.${expv_4}"
!define PROG_NAME "Ozonesonde Viewer 2019"

##########################################################################################▲

#the name of the installer
Name "${PROG_NAME} ${PROG_VERSION}"

#the file to write
OutFile "${PROG_NAME} Setup ${PROG_VERSION}.exe"

#the default installation directory
InstallDir "$PROGRAMFILES\${PROG_NAME}"

#get the previous installation folder from the registry if available
InstallDirRegKey HKLM "Software\${PROG_NAME}" "Install_Dir"

#request application privileges for Windows Vista/7
RequestExecutionLevel admin

##########################################################################################▼
ComponentText "Check the components you want to install.  This program requires Microsoft's .NET Framework 4.7.2 or higher.  Click Next to continue."
##########################################################################################▲

#opening/starting page
!insertmacro MUI_PAGE_WELCOME
#list any optional installer sections
!insertmacro MUI_PAGE_COMPONENTS
#set the installation directory
!insertmacro MUI_PAGE_DIRECTORY
#install the selected sections
!insertmacro MUI_PAGE_INSTFILES

#list the uninstaller pages
!insertmacro MUI_UNPAGE_CONFIRM
!insertmacro MUI_UNPAGE_INSTFILES

!insertmacro MUI_LANGUAGE "English"

#mandatory section installing the program's files
Section "${PROG_NAME} Program (required)" programSectionDescription
	
	#set the context of $SMPROGRAMS and other shell folders for the current user
	SetShellVarContext current
	
	#make this a "read only" section (aka mandatory)
	SectionIn RO
	#set output path to the installation directory.
	SetOutPath $INSTDIR
	
##########################################################################################▼
	File "${EXEC_PATH}\${EXEC_NAME}"
	File "${EXEC_PATH}\${EXEC_NAME}.config"
	File "${EXEC_PATH}\*.dll"
	File "${EXEC_PATH}\bright_rainbow.csv"
	File "${EXEC_PATH}\instrument selection graphic.png"
	File "${EXEC_PATH}\pumpEff.xml"
	
	#library license files:
	#File "zedgraph license.txt"
	#File "mathdotnet numerics license.txt"
##########################################################################################▲
	
	#write the uninstall keys for Windows
	WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${PROG_NAME}" "DisplayName" "${PROG_NAME}"
	WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${PROG_NAME}" "UninstallString" '"$INSTDIR\uninstall.exe"'
	WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${PROG_NAME}" "DisplayVersion" "${PROG_VERSION}"
	WriteRegDWORD HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${PROG_NAME}" "NoModify" 1
	WriteRegDWORD HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${PROG_NAME}" "NoRepair" 1
	
	#store the installation folder in the registry (checked by future upgrade installations)
	WriteRegStr HKLM "Software\${PROG_NAME}" "Install_Dir" "$INSTDIR"
	
	#create the uninstaller
	WriteUninstaller "uninstall.exe"
SectionEnd

/*
#optional section for installing the source code
Section "Source Code" sourceSectionDescription
	#set the context of $SMPROGRAMS and other shell folders to install for all users
	SetShellVarContext all
	
	#set output path to the installation directory.
	SetOutPath $INSTDIR\src
	
##########################################################################################▼
	File *.cs
	File *.resx
	File *.xml
	File *.ico
##########################################################################################▲
	
	#revert back to the normal output path
	SetOutPath $INSTDIR
SectionEnd
*/

#optional section for adding start-menu shortcuts to the program
Section "Start Menu Shortcuts" startMenuSectionDescription
	#set the context of $SMPROGRAMS and other shell folders to install for all users
	SetShellVarContext all
	
	CreateDirectory "$SMPROGRAMS\${PROG_NAME}"
	CreateShortCut "$SMPROGRAMS\${PROG_NAME}\Uninstall.lnk" "$INSTDIR\uninstall.exe" "" "$INSTDIR\uninstall.exe" 0
	CreateShortCut "$SMPROGRAMS\${PROG_NAME}\${PROG_NAME}.lnk" "$INSTDIR\${EXEC_NAME}" "" "$INSTDIR\${EXEC_NAME}" 0
	
SectionEnd

#optional section for adding desktop shortcuts to the program
Section "Desktop Shortcut" desktopSectionDescription
	#set the context of $SMPROGRAMS and other shell folders to install for all users
	SetShellVarContext all
	
	CreateShortCut "$DESKTOP\${PROG_NAME}.lnk" "$INSTDIR\${EXEC_NAME}"
SectionEnd


#define the cursor-hover descriptions for the component sections
LangString DESC_programSectionDescription ${LANG_ENGLISH} "Install the required files for ${PROG_NAME} to run."
#LangString DESC_sourceSectionDescription ${LANG_ENGLISH} "Install the source code for this program."
LangString DESC_startMenuSectionDescription ${LANG_ENGLISH} "Add shortcuts to the start menu."
LangString DESC_desktopSectionDescription ${LANG_ENGLISH} "Add a shortcut to the desktop."

#attach the descriptions to each component section
!insertmacro MUI_FUNCTION_DESCRIPTION_BEGIN
  !insertmacro MUI_DESCRIPTION_TEXT ${programSectionDescription} $(DESC_programSectionDescription)
  #!insertmacro MUI_DESCRIPTION_TEXT ${sourceSectionDescription} $(DESC_sourceSectionDescription)
  !insertmacro MUI_DESCRIPTION_TEXT ${startMenuSectionDescription} $(DESC_startMenuSectionDescription)
  !insertmacro MUI_DESCRIPTION_TEXT ${desktopSectionDescription} $(DESC_desktopSectionDescription)
!insertmacro MUI_FUNCTION_DESCRIPTION_END

#specify which files/directories/etc to remove when uninstalling the program
Section "un.Uninstaller Section"
	#set the context of $SMPROGRAMS and other shell folders for all users
	SetShellVarContext all
	
	#remove start-menu shortcuts, if any
	Delete "$SMPROGRAMS\${PROG_NAME}\*.*"
	RMDir "$SMPROGRAMS\${PROG_NAME}"
	
	#remove desktop shortcut, if any
	Delete "$DESKTOP\${PROG_NAME}.lnk"
	
	#remove the uninstallation registry key
	DeleteRegKey HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${PROG_NAME}"
	#remove the installation folder reference key
	DeleteRegKey HKLM "Software\${PROG_NAME}"

	#recursively remove the entire installation directory
	RMDir /r $INSTDIR
	
	#set the context of $SMPROGRAMS and other shell folders for the current user
	SetShellVarContext current
	#remove any remaining user-level shortcuts (possibly left over from previous SkySonde installations)
	Delete "$SMPROGRAMS\${PROG_NAME}\*.*"
	RMDir "$SMPROGRAMS\${PROG_NAME}"
	Delete "$DESKTOP\${PROG_NAME}.lnk"
	
SectionEnd
