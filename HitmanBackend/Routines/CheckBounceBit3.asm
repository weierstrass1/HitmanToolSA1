CheckBounceBit3:
    JSL CheckBounce
    LDA $65
    BMI .Return
    LDA #$04
    TSB $4F
.Return
RTL
