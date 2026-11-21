CheckBounceBit2:
    JSL CheckBounce
    LDA $65
    BMI .Return
    LDA #$02
    TSB $4F
.Return
RTL
