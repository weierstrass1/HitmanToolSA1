; Shared DX/Dyzen symbols (!Sprite*, !Extended*, !Player*) are supplied by
; the configured PIXI ExtraDefines. Only missing SMW globals are named here.
!HitmanOtherSpriteIndex = $1695|!addr
!HitmanQuakeIndex = $1698|!addr
!HitmanFireballIndex = $185E|!addr
!HitmanQuakeStatus = $16CD|!addr
!HitmanQuakeTimer = $18F8|!addr
!HitmanQuakeXLow = $16D1|!addr
!HitmanQuakeXHigh = $16D5|!addr
!HitmanQuakeYLow = $16D9|!addr
!HitmanQuakeYHigh = $16DD|!addr
!HitmanGroundShake = $1887|!addr
!HitmanCapeGroundTimer = $14A9|!addr
!HitmanCapeActive = $13E8|!addr
!HitmanCapeX = $13E9|!addr
!HitmanCapeY = $13EB|!addr
!HitmanSound1 = $1DF9|!addr
!HitmanSound3 = $1DFC|!addr
!HitmanBehindScenery = $13F9|!addr
!HitmanSpecialKillFlag = $1549|!addr
!HitmanVanillaSpriteToSpawn = $01A7C9|!rom


; M=X=1, X=receiver. Constant event mask.
macro HitmanMarkAction(event)
    LDA !Hitman_Prop_VanillaEventMask,x
    AND #<event>
    BEQ ?done
    ORA !Hitman_Prop_ActionFlag,x
    STA !Hitman_Prop_ActionFlag,x
?done:
endmacro

; A = row byte base, X = lower slot. $01 = $00/$FF for disable/enable.
; Clobbers A/X/Y and $00; independent of DBR.
macro HitmanWritePairBit()
    CLC
    ADC.l HitmanPairSlotByte,x
    TAY
    LDA.l HitmanPairBit,x
    STA $00
    TYX
    LDA.l !HitmanPairMask,x
    EOR $01
    AND $00
    EOR.l !HitmanPairMask,x
    STA.l !HitmanPairMask,x
endmacro
