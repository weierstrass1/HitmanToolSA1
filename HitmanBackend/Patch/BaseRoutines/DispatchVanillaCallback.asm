; A=event, X=receiver, Y=source, M=X=1. Called after the response.
; Preserves X/Y, DBR/D and $0E. Callback flags are ignored.
DispatchVanillaCallback:
    PHA
    CMP #$81
    BCS .Skip
    AND !Hitman_Prop_VanillaEventMask,x
    BEQ .Skip
    LDA !Hitman_Prop_VanillaCallbackID,x
    BNE .Lookup
.Skip
    PLA
    RTL
.Lookup
    PHB
    PHX
    PHY
    LDA $04,s
    PHA
    LDA !Hitman_Prop_VanillaCallbackID,x
    PHA
    LDA $0E
    PHA
    ; Select the callback's byte offset. Explosion retains the legacy slot.
    LDA $03,s
    CMP #!HitmanEventExplosion
    BEQ .Explosion
    LSR A
    LSR A
    LSR A
    TAX
    LDA.l .EventOffsets,x
    BRA .Offset
.Explosion
    LDA #$0F
.Offset
    STA $00
    STZ $01
    REP #$30
    LDA $02,s
    AND #$00FF
    ASL A
    STA $02
    ASL A
    ASL A
    ASL A
    CLC
    ADC $02                  ; 18 bytes per profile (six long pointers).
    ADC $00
    TAX
    LDA.l HitmanVanillaCallbackTable,x
    STA !HitmanRoutineAddress
    LDA.l HitmanVanillaCallbackTable+1,x
    STA !HitmanRoutineAddress+1
    SEP #$30
    LDA !HitmanRoutineAddress
    ORA !HitmanRoutineAddress+1
    ORA !HitmanRoutineAddress+2
    BEQ .Restore
    LDA !HitmanRoutineAddress+2
    PHA
    PLB
    LDA $05,s
    TAX
    LDA $04,s
    TAY
    LDA $03,s
    JSR .CallCallback
.Restore
    PLA
    STA $0E
    PLA
    PLA
    PLY
    PLX
    PLB
    PLA
    RTL
.CallCallback
    PHK
    PEA.w .CallbackReturned-1
    JML [!HitmanRoutineAddress|!dp]
.CallbackReturned
    RTS

; Indexed by the single event bit shifted right three places.
.EventOffsets
    db $00,$03,$06,$00,$09,$00,$00,$00
    db $0C,$00,$00,$00,$00,$00,$00,$00
    db $00
