if read1($00FFD5) == $23
    fullsa1rom
    !sa1 = 1
    !rom = $000000
else
    lorom
    !sa1 = 0
    !rom = $800000
endif

incsrc "Files/HitmanGeneralDefines.asm"
incsrc "Files/HitmanDefines.asm"
incsrc "Files/ExtraDefinesIncludes.asm"
incsrc "../ASM/VanillaInteractionDefines.asm"

; The reservation ceiling is advisory. Invalid memory and known DX overlaps
; must still be rejected, regardless of the configured reservation size.
assert !HitmanRAMLimit > !HitmanRAMBase, "Invalid Hitman WRAM reservation."
assert (!HitmanRAMBase>>16) == ((!HitmanRAMLimit-1)>>16), "Hitman WRAM reservation crosses a bank."
assert (!HitmanRAMBase>>16) == ((!HitmanRAMEnd-1)>>16), "Hitman variables cross a WRAM bank."
if !sa1
    assert !HitmanRAMBase >= $400000 && !HitmanRAMEnd <= $440000, "Hitman variables must use SA-1 BW-RAM."
else
    assert !HitmanRAMBase >= $7E0000 && !HitmanRAMEnd <= $800000, "Hitman variables must use SNES WRAM."
    if defined("Variables3")
        ; Includes use the older third DX block when it starts at $7FAC82.
        if !Variables3 == $7FAC82
            assert !HitmanRAMEnd <= $7FAC82 || !HitmanRAMBase >= $7FAE56, "Hitman overlaps the older Dynamic Xtreme sprite tables at $7FAC82. Select a different reservation in ASM/HitmanDefines.asm."
        endif
    endif
    if defined("Variables2") && defined("PalettesEffects")
        if !Variables2 == $7F9C80 && !PalettesEffects
            assert !HitmanRAMEnd <= $7F9C80 || !HitmanRAMBase >= $7FAB00, "Hitman overlaps the Dynamic Xtreme palette buffers. Select a different reservation in ASM/HitmanDefines.asm."
        endif
    endif
endif
if !HitmanRAMEnd > !HitmanRAMLimit
    warn "Hitman variables exceed the configured WRAM reservation."
    print "Hitman WRAM excess: ", dec(!HitmanRAMEnd-!HitmanRAMLimit), " bytes."
endif
print "Hitman WRAM (", "!HitmanRAMLayout", "): $", hex(!HitmanRAMBase), "-$", hex(!HitmanRAMEnd-1), " (", dec(!HitmanRAMEnd-!HitmanRAMBase), " bytes)."

incsrc "Interactions/SpriteSprite.asm"
incsrc "Interactions/Fireball.asm"
incsrc "Interactions/Explosion.asm"
incsrc "Interactions/Quake.asm"
incsrc "Interactions/CapeDive.asm"
incsrc "Interactions/CarriedDraw.asm"

org $07F7D2|!rom
    autoclean JML InitSpriteTables
    dl RoutinesTable

freecode
prot HitBoxXOffsetTables, ExtendedHitboxSystem

InitSpriteTables:
    PHB
    PHK
    PLB
    PHY
    JSR CustomPropertiesSetup
    ; The vanilla initializer is used by spawn routines that pass scratch data.
    PEI ($00)
    PEI ($02)
    LDA #$01
    JSL Hitman_Base_SetSpriteSlotCollision
    REP #$20
    PLA
    STA $02
    PLA
    STA $00
    SEP #$20
    PLY
    PLB
    JSL $07F722|!rom
    JSL $07F78B|!rom
RTL
incsrc "Files/CustomPropertiesSetup.asm"

; A vanilla transformation can reload tweakers without clearing all sprite
; tables. Refresh its Hitman configuration without changing that behavior.
HitmanRefreshCustomProperties:
    PHB
    PHK
    PLB
    PHY
    JSR CustomPropertiesSetup
    PLY
    PLB
    RTL

incsrc "Files/CustomPropertiesTables.asm"
incsrc "Files/VanillaCallbacks.asm"

RoutinesTable:
    incsrc "Files/RoutinesTable.asm"

; Vanilla status handlers return with RTS in bank 01. Return through the
; original PLB : RTL at the end of the long sprite interaction entry.
assert read2($018038|!rom) == $6BAB, "Hitman requires the bank 01 PLB/RTL return"
macro HitmanCallStatus(target)
    PHB
    LDA.b #(($018038|!rom)>>16)
    PHA
    PLB
    PEA.w $8037
    JML <target>|!rom
endmacro

incsrc "Files/Includes.asm"

freedata
HitBoxXOffsetTables:
    dw HitBoxXOffsets,HitBoxXOffsetsFlip,HitBoxXOffsets,HitBoxXOffsetsFlip
HitBoxYOffsetTables:
    dw HitBoxYOffsets,HitBoxYOffsets,HitBoxYOffsetsFlip,HitBoxYOffsetsFlip
incsrc "Files/Data.asm"
HitboxDataEnd:
assert bank(HitBoxXOffsetTables) == bank(HitboxDataEnd-1), "Hitbox data crosses a ROM bank"

freedata
incsrc "../ASM/ExtendedSpriteTables.asm"
