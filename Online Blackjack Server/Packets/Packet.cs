﻿namespace Online_Blackjack_Server.Packets
{
    enum Packet : ushort
    {
        PING = 0,
        REGISTER_USERNAME = 1,
        REQUEST_RESPONSE = 2,
        CONTINUE_PLAYING = 3,
        REMOVE_FROM_GAME_SESSION = 4,
        REQUEST_BET = 5,
        REQUEST_PLAYER_INFO = 6,
        LEAVE_SESSION = 7,
        PLACE_BET = 8,
        STAND = 9,
        HIT = 10,
        INVALID_BET = 11,
        VALID_BET = 12,
        END_TURN = 13,
        BUST = 14,
        BLACKJACK = 15,
        JOIN_TWO_PLAYER_QUEUE = 16,
        JOIN_THREE_PLAYER_QUEUE = 17,
        JOIN_FOUR_PLAYER_QUEUE = 18,
        JOIN_SOLO_SESSION = 19,
        PLAYER_INFO = 20,
        DEALER_INFO = 21,
        USERNAME_REGISTERED = 22,
        VERSION_INFO = 23,
        LOBBY_INFO = 24,
        REQUEST_LOBBY_INFO = 25,
        LEAVE_MATCHMAKING = 26,
        SESSION_STARTED_SOLO = 27,
        SESSION_STARTED_TWO = 28,
        SESSION_STARTED_THREE = 29,
        SESSION_STARTED_FOUR = 30,
        FINISH_TAKING_RESPONSES = 31,
        NO_MORE_RESPONSES = 32,
        BEAT_THE_DEALER = 33,
        LOST_AGAINST_DEALER = 34,
        TIE_WITH_DEALER = 35,
        DELAY_TIME_BEFORE_GAME = 36,
        DELAY_TIME_AFTER_GAME = 37,
        DELAY_TIME_FOR_DEALER = 38,
        OTHER_PLAYER_INFO = 39,
        RESEND_LAST_PACKET = 40,
        REMOVE_PLAYER_FROM_CLIENT = 41,

    }
}