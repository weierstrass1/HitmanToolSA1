    !dp = $0000
    !addr = $0000
    !rom = $800000
    !MaxSprites = $0C
    !SpriteBeingEaten = $15D0
    !SpriteBehindScenery = $1632

if !sa1 == 1
    !dp = $3000
    !addr = $6000
    !rom = $000000
    !MaxSprites = $16
    !SpriteBeingEaten = $754C
    !SpriteBehindScenery = $75A4
endif

macro VanillaInteractionFlag()
!VanillaInteractions = (!UseCapeStompingInteraction<<6)|(!UseYoshiStompingInteraction<<5)|(!UseCapeInteraction<<4)|(!UseYoshiFireballInteraction<<3)|(!UseFireballInteraction<<2)|(!UseBobombInteraction<<1)|(!UseSpriteInteraction)
endmacro

!MaxClusters = $14
!MaxExtended = $0A

!FireballStoppingIndex = !MaxExtended-2

!HitmanSprite1Context = $53
!HitmanSprite1Flip = $4E
!HitmanSprite1X = $59
!HitmanSprite1Y = $51

!HitmanSprite2Context = $04
!HitmanSprite2Flip = $50
!HitmanSprite2X = $8A
!HitmanSprite2Y = $8C

!HitmanHB1Left = $45
!HitmanHB1Top = $47
!HitmanHB1Right = $49
!HitmanHB1Bottom = $4B

!HitmanHB2Left = $00
!HitmanHB2Top = $08
!HitmanHB2Right = $02
!HitmanHB2Bottom = $0C

!HitmanHitboxXOffset = $98
!HitmanHitboxYOffset = $9A

!HitmanRoutineAddress = $D5

!HitmanHBStatus = $4D
!HitmanDefault = $4F

; For a project override, define both values here. The limit is exclusive.
; !HitmanRAMBase = $7FA440
; !HitmanRAMLimit = $7FA660
if defined("HitmanRAMBase") != defined("HitmanRAMLimit")
    error "Define both HitmanRAMBase and HitmanRAMLimit for a custom WRAM reservation."
endif

if defined("HitmanRAMBase")
    !HitmanRAMLayout = "project override"
elseif !sa1
    !HitmanRAMLayout = "SA-1"
    !HitmanRAMBase = $408000
    !HitmanRAMLimit = $409400
; Check the shared GFX relocation, without relying on DX's private marker.
elseif read1($00B888) == $60 && read3($00F649) == $800069 && read3($00F667) == $800069 && read3($01E1A8) == $880069 && read1($05BB8F) == $5C
    !HitmanRAMLayout = "LoROM / Free7E2000"
    !HitmanRAMBase = $7E7000
    !HitmanRAMLimit = $7E7D00
else
    !HitmanRAMLayout = "LoROM"
    !HitmanRAMBase = $7FAC40
    !HitmanRAMLimit = $7FAF00
endif

; Context and variable offsets intentionally share storage: systems 2/4 and 3
; are mutually exclusive. Each sprite family starts after the preceding one.
!HitmanNormalContextIDHigh = !HitmanRAMBase
!HitmanNormalContextIDLow = !HitmanNormalContextIDHigh+!MaxSprites
!HitmanNormalXOffHigh = !HitmanNormalContextIDHigh
!HitmanNormalXOffLow = !HitmanNormalXOffHigh+!MaxSprites
!HitmanNormalYOffHigh = !HitmanNormalXOffLow+!MaxSprites
!HitmanNormalYOffLow = !HitmanNormalYOffHigh+!MaxSprites
!HitmanNormalWidthHigh = !HitmanNormalYOffLow+!MaxSprites
!HitmanNormalWidthLow = !HitmanNormalWidthHigh+!MaxSprites
!HitmanNormalHeightHigh = !HitmanNormalWidthLow+!MaxSprites
!HitmanNormalHeightLow = !HitmanNormalHeightHigh+!MaxSprites
!HitmanNormalPlayerIsAbove = !HitmanNormalHeightLow+!MaxSprites
!HitmanNormalFlip = !HitmanNormalPlayerIsAbove+!MaxSprites

!HitmanClusterContextIDHigh = !HitmanNormalFlip+!MaxSprites
!HitmanClusterContextIDLow = !HitmanClusterContextIDHigh+!MaxClusters
!HitmanClusterXOffHigh = !HitmanClusterContextIDHigh
!HitmanClusterXOffLow = !HitmanClusterXOffHigh+!MaxClusters
!HitmanClusterYOffHigh = !HitmanClusterXOffLow+!MaxClusters
!HitmanClusterYOffLow = !HitmanClusterYOffHigh+!MaxClusters
!HitmanClusterWidthHigh = !HitmanClusterYOffLow+!MaxClusters
!HitmanClusterWidthLow = !HitmanClusterWidthHigh+!MaxClusters
!HitmanClusterHeightHigh = !HitmanClusterWidthLow+!MaxClusters
!HitmanClusterHeightLow = !HitmanClusterHeightHigh+!MaxClusters
!HitmanClusterPlayerIsAbove = !HitmanClusterHeightLow+!MaxClusters
!HitmanClusterFlip = !HitmanClusterPlayerIsAbove+!MaxClusters

!HitmanExtendedContextIDHigh = !HitmanClusterFlip+!MaxClusters
!HitmanExtendedContextIDLow = !HitmanExtendedContextIDHigh+!MaxExtended
!HitmanExtendedXOffHigh = !HitmanExtendedContextIDHigh
!HitmanExtendedXOffLow = !HitmanExtendedXOffHigh+!MaxExtended
!HitmanExtendedYOffHigh = !HitmanExtendedXOffLow+!MaxExtended
!HitmanExtendedYOffLow = !HitmanExtendedYOffHigh+!MaxExtended
!HitmanExtendedWidthHigh = !HitmanExtendedYOffLow+!MaxExtended
!HitmanExtendedWidthLow = !HitmanExtendedWidthHigh+!MaxExtended
!HitmanExtendedHeightHigh = !HitmanExtendedWidthLow+!MaxExtended
!HitmanExtendedHeightLow = !HitmanExtendedHeightHigh+!MaxExtended
!HitmanExtendedPlayerIsAbove = !HitmanExtendedHeightLow+!MaxExtended
!HitmanExtendedFlip = !HitmanExtendedPlayerIsAbove+!MaxExtended

!HitmanCustomProperties = !HitmanExtendedFlip+!MaxExtended

!HitmanEventShell = $01
!HitmanEventRegular = $02
!HitmanEventExplosion = $04
!HitmanEventFireball = $08
!HitmanEventYoshiFireball = $10
!HitmanEventQuake = $20
!HitmanEventCapeDive = $40
!HitmanEventShellHit = $80
