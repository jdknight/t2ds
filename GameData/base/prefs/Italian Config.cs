// Tribes 2 Input Map File
moveMap.clearMap();
moveMap.bindCmd(keyboard, "escape", "", "escapeFromGame();");
moveMap.bind(keyboard, "alt e", toggleEditor);
moveMap.bind(keyboard, "s", moveleft);
moveMap.bind(keyboard, "f", moveright);
moveMap.bind(keyboard, "e", moveforward);
moveMap.bind(keyboard, "d", movebackward);
moveMap.bind(keyboard, "space", jump);
moveMap.bind(keyboard, "shift a", altTrigger);
moveMap.bind(keyboard, "pageup", pageMessageHudUp);
moveMap.bind(keyboard, "pagedown", pageMessageHudDown);
moveMap.bind(keyboard, "x", voiceCapture);
moveMap.bind(keyboard, "shift w", prevWeapon);
moveMap.bind(keyboard, "w", nextWeapon);
moveMap.bind(keyboard, "z", toggleFreeLook);
moveMap.bind(keyboard, "q", useRepairKit);
moveMap.bind(keyboard, "r", useBackPack);
moveMap.bind(keyboard, "1", useFirstWeaponSlot);
moveMap.bind(keyboard, "2", useSecondWeaponSlot);
moveMap.bind(keyboard, "3", useThirdWeaponSlot);
moveMap.bind(keyboard, "4", useFourthWeaponSlot);
moveMap.bind(keyboard, "5", useFifthWeaponSlot);
moveMap.bind(keyboard, "6", useSixthWeaponSlot);
moveMap.bind(keyboard, "g", throwGrenade);
moveMap.bind(keyboard, "b", placeMine);
moveMap.bind(keyboard, "h", placeBeacon);
moveMap.bind(keyboard, "l", useTargetingLaser);
moveMap.bind(keyboard, "ctrl w", throwWeapon);
moveMap.bind(keyboard, "ctrl r", throwPack);
moveMap.bind(keyboard, "ctrl f", throwFlag);
moveMap.bind(keyboard, "t", setZoomFOV);
moveMap.bind(keyboard, "a", toggleZoom);
moveMap.bind(keyboard, "numpadenter", toggleInventoryHud);
moveMap.bind(keyboard, "numpad1", selectFavorite1);
moveMap.bind(keyboard, "numpad2", selectFavorite2);
moveMap.bind(keyboard, "numpad3", selectFavorite3);
moveMap.bind(keyboard, "numpad4", selectFavorite4);
moveMap.bind(keyboard, "numpad5", selectFavorite5);
moveMap.bind(keyboard, "numpad6", selectFavorite6);
moveMap.bind(keyboard, "numpad7", selectFavorite7);
moveMap.bind(keyboard, "numpad8", selectFavorite8);
moveMap.bind(keyboard, "numpad9", selectFavorite9);
moveMap.bind(keyboard, "numpad0", selectFavorite10);
moveMap.bind(keyboard, "shift numpad1", selectFavorite11);
moveMap.bind(keyboard, "shift numpad2", selectFavorite12);
moveMap.bind(keyboard, "shift numpad3", selectFavorite13);
moveMap.bind(keyboard, "shift numpad4", selectFavorite14);
moveMap.bind(keyboard, "shift numpad5", selectFavorite15);
moveMap.bind(keyboard, "shift numpad6", selectFavorite16);
moveMap.bind(keyboard, "shift numpad7", selectFavorite17);
moveMap.bind(keyboard, "shift numpad8", selectFavorite18);
moveMap.bind(keyboard, "shift numpad9", selectFavorite19);
moveMap.bind(keyboard, "shift numpad0", selectFavorite20);
moveMap.bind(keyboard, "ctrl numpad0", quickPackEnergyPack);
moveMap.bind(keyboard, "ctrl numpad1", quickPackRepairPack);
moveMap.bind(keyboard, "ctrl numpad2", quickPackShieldPack);
moveMap.bind(keyboard, "ctrl numpad3", quickPackCloakPack);
moveMap.bind(keyboard, "ctrl numpad4", quickPackJammerPack);
moveMap.bind(keyboard, "ctrl numpad5", quickPackAmmoPack);
moveMap.bind(keyboard, "ctrl numpad6", quickPackSatchelCharge);
moveMap.bind(keyboard, "ctrl numpad7", quickPackDeployableStation);
moveMap.bind(keyboard, "ctrl numpad8", quickPackIndoorTurret);
moveMap.bind(keyboard, "ctrl numpad9", quickPackOutdoorTurret);
moveMap.bind(keyboard, "ctrl numpaddivide", quickPackMotionSensor);
moveMap.bind(keyboard, "ctrl *", quickPackPulse);
moveMap.bind(keyboard, "tab", toggleFirstPerson);
moveMap.bind(keyboard, "u", ToggleMessageHud);
moveMap.bind(keyboard, "y", TeamMessageHud);
moveMap.bind(keyboard, "v", activateChatMenuHud);
moveMap.bind(keyboard, "c", toggleCommanderMap);
moveMap.bind(keyboard, "ctrl k", suicide);
moveMap.bind(keyboard, "f1", toggleHelpGui);
moveMap.bind(keyboard, "f2", toggleScoreScreen);
moveMap.bind(keyboard, "f6", toggleHudWaypoints);
moveMap.bind(keyboard, "f7", toggleHudMarkers);
moveMap.bind(keyboard, "f8", toggleHudTargets);
moveMap.bind(keyboard, "f9", toggleHudCommands);
moveMap.bind(keyboard, "n", toggleTaskListDlg);
moveMap.bind(keyboard, "return", fnAcceptTask);
moveMap.bind(keyboard, "backspace", fnDeclineTask);
moveMap.bind(keyboard, "shift c", fnTaskCompleted);
moveMap.bind(keyboard, "shift x", fnResetTaskList);
moveMap.bind(keyboard, "insert", voteYes);
moveMap.bind(keyboard, "delete", voteNo);
moveMap.bind(keyboard, "left", turnLeft);
moveMap.bind(keyboard, "right", turnRight);
moveMap.bind(keyboard, "up", panUp);
moveMap.bind(keyboard, "down", panDown);
moveMap.bind(keyboard, "p", resizeChatHud);
moveMap.bind(keyboard, "ctrl n", toggleNetDisplayHud);
moveMap.bind(mouse0, "xaxis", yaw);
moveMap.bind(mouse0, "yaxis", pitch);
moveMap.bind(mouse0, "button0", mouseFire);
moveMap.bind(mouse0, "button1", mouseJet);
moveMap.bind(mouse0, "zaxis", cycleWeaponAxis);
observerMap.clearMap();
observerMap.bind(keyboard, "space", jump);
observerMap.bind(keyboard, "up", moveup);
observerMap.bind(keyboard, "down", movedown);
observerMap.bind(mouse0, "button1", mouseJet);
GlobalActionMap.bind(keyboard, "backslash", toggleConsole);
