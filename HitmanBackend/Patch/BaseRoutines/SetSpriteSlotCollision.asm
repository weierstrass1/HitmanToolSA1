; A=0 disables, A=1 enables; X is a normal sprite slot.
; M=X=1. Preserves X/Y/DBR/D, uses $00-$02. Invalid slots are ignored.
macro HitmanSlotCollisionRow(prefix, operation, row)
.<prefix><row>:
    ; Sum ceil(i/8) over the preceding rows.
    !slotCollisionPrevious #= <row>-1
    !slotCollisionGroups #= !slotCollisionPrevious>>3
    !slotCollisionBase #= (!slotCollisionGroups+1)*(!slotCollisionPrevious-(4*!slotCollisionGroups))
    LDA.l !HitmanPairMask+!slotCollisionBase,x
    <operation> $00
    STA.l !HitmanPairMask+!slotCollisionBase,x
endmacro

SetSpriteSlotCollision:
    CPX.b #!MaxSprites
    BCC .ValidSlot
    RTL
.ValidSlot
    PHX
    PHY
    AND #$01
    BEQ .State
    LDA #$FF
.State
    STA $01
    LDA.l HitmanPairSlotByte,x
    STA $02
    BNE .OwnRow
    TXA
    BEQ .PrepareHigher
.OwnRow
    LDA.l HitmanPairBit,x
    DEC A
    STA $00
    LDA.l HitmanPairRowBase,x
    TAX
    LDY $02
    BEQ .Partial
    LDA $01
    CPY #$02
    BEQ .TwoBytes
    STA.l !HitmanPairMask,x
    INX
    BRA .Partial
.TwoBytes
    XBA
    LDA $01
    REP #$20
    STA.l !HitmanPairMask,x
    SEP #$20
    INX
    INX
.Partial
    LDA $00
    BEQ .PrepareHigher
    BIT $01
    BMI .EnablePartial
    EOR #$FF
    AND.l !HitmanPairMask,x
    BRA .StorePartial
.EnablePartial
    ORA.l !HitmanPairMask,x
.StorePartial
    STA.l !HitmanPairMask,x
.PrepareHigher
    LDA $02,s
    CMP.b #!MaxSprites-1
    BEQ .Restore
    TAX
    ASL A
    TAY
    LDA.l HitmanPairBit,x
    BIT $01
    BMI .EnableHigher
    EOR #$FF
    STA $00
    TYX
    REP #$20
    LDA.l .DisableTargets,x
    BRA .EnterRows
.EnableHigher
    STA $00
    TYX
    REP #$20
    LDA.l .EnableTargets,x
.EnterRows
    ; RTS consumes only the synthetic return address. The caller used JSL.
    PHA
    SEP #$20
    LDX $02
    RTS
.Restore
    PLY
    PLX
    RTL

.DisableTargets
    !slotCollisionRow = 1
    while !slotCollisionRow < !MaxSprites
        dw .DisableRow!slotCollisionRow-1
        !slotCollisionRow #= !slotCollisionRow+1
    endwhile
.EnableTargets
    !slotCollisionRow = 1
    while !slotCollisionRow < !MaxSprites
        dw .EnableRow!slotCollisionRow-1
        !slotCollisionRow #= !slotCollisionRow+1
    endwhile

    !slotCollisionRow = 1
    while !slotCollisionRow < !MaxSprites
        %HitmanSlotCollisionRow(DisableRow, AND, !slotCollisionRow)
        !slotCollisionRow #= !slotCollisionRow+1
    endwhile
    JMP .Restore

    !slotCollisionRow = 1
    while !slotCollisionRow < !MaxSprites
        %HitmanSlotCollisionRow(EnableRow, ORA, !slotCollisionRow)
        !slotCollisionRow #= !slotCollisionRow+1
    endwhile
    JMP .Restore

.End
assert bank(SetSpriteSlotCollision) == bank(.End-1), "Slot collision dispatch crosses a ROM bank"

