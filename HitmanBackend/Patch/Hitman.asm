if read1($00FFD5) == $23
    fullsa1rom
    !sa1 = 1
else
    lorom
    !sa1 = 0
endif

org $07F7D2
    autoclean JML InitSpriteTables|!rom
    dl RoutinesTable

freecode
prot HitBoxXOffsetTables

incsrc "Files/HitmanGeneralDefines.asm"
incsrc "Files/HitmanDefines.asm"

InitSpriteTables:
    JSL $07F722|!rom
    JSL $07F78B|!rom
    PHB
    PHK
    PLB
    JSR CustomPropertiesSetup
    PLB
RTL
incsrc "Files/CustomPropertiesSetup.asm"

incsrc "Files/CustomPropertiesTables.asm"

RoutinesTable:
    incsrc "RoutinesTable.asm"

incsrc "Files/Includes.asm"

freedata
HitBoxXOffsetTables:
    dw HitBoxXOffsets,HitBoxXOffsetsFlip,HitBoxXOffsets,HitBoxXOffsetsFlip
HitBoxYOffsetTables:
    dw HitBoxYOffsets,HitBoxYOffsetsFlip,HitBoxYOffsets,HitBoxYOffsetsFlip
incsrc "Files/Data.asm"
