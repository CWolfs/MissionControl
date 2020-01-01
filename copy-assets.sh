#!/bin/bash

rm -rf 'D:/Program Files (x86)/Steam/steamapps/common/BATTLETECH/Mods/MissionControl/contractTypeBuilds'
rm -rf 'D:/Program Files (x86)/Steam/steamapps/common/BATTLETECH/Mods/MissionControl/overrides'

cp -r 'contractTypeBuilds/' 'D:/Program Files (x86)/Steam/steamapps/common/BATTLETECH/Mods/MissionControl'
cp -r 'overrides/' 'D:/Program Files (x86)/Steam/steamapps/common/BATTLETECH/Mods/MissionControl'
cp -r 'mod.json' 'D:/Program Files (x86)/Steam/steamapps/common/BATTLETECH/Mods/MissionControl'
cp -r 'settings.json' 'D:/Program Files (x86)/Steam/steamapps/common/BATTLETECH/Mods/MissionControl'
cp -r 'config' 'D:/Program Files (x86)/Steam/steamapps/common/BATTLETECH/Mods/MissionControl'