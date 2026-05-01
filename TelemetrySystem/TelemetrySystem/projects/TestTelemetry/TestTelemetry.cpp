// TestTelemetry.cpp : Este archivo contiene la función "main". La ejecución del programa comienza y termina ahí.
//

#define CATCH_CONFIG_MAIN
#include<catch.hpp>
#include<filesystem>
#include"TelemetrySystem.h"
#include"Tracker.h"
//#include"EventData.h"


TEST_CASE( "Correct Tracker Init", "[Tracker]" ) {

    Tracker* tracker = (Tracker*) CreateTracker( 0, 0, 0, "DataInit.json" );
    REQUIRE( tracker != nullptr );
    REQUIRE( std::filesystem::exists( "DataInit.json" ) );
    CloseTracker( tracker );
};

TEST_CASE( "Wrong Tracker Init", "[Tracker]" ) {

    Tracker* tracker = (Tracker*)CreateTracker( 5, 2, 6, "DataTest.json" );
    REQUIRE( tracker == nullptr );
    REQUIRE( !std::filesystem::exists( "DataTest.json" ) );
    CloseTracker( tracker );
};

TEST_CASE( "Create Evnet Correct", "[Evnets]" ) {

    Tracker* tracker = (Tracker*)CreateTracker( 0, 0, 0, "EventDataTest.json" );
    EventData* data = CreateTelemetryEvent( 0 );
    REQUIRE( data != nullptr );
    CloseTracker( tracker );
};

TEST_CASE( "Create Evnet Wrong", "[Evnets]" ) {

    Tracker* tracker = (Tracker*)CreateTracker( 0, 0, 0, "EventDataTest.json" );
    EventData* data = CreateTelemetryEvent( -5 );
    REQUIRE( data == nullptr );
    CloseTracker( tracker );
};


TEST_CASE( "Track Evnet Correct", "[Evnets]" ) {

    Tracker* tracker = (Tracker*)CreateTracker( 0, 0, 0, "EventDataTest.json" );
    EventData* data = CreateTelemetryEvent( 0 );
    EventData* data2 = CreateTelemetryEvent( 0 );
    int ret = TrackEvent( tracker,data );
    int ret2 = TrackEvent( tracker,data2 );// NOTA: si intentas trackear 2 veces un mismo puntero la libreria revienta (destruccion de un bloque ya destruido)
    REQUIRE( (ret ==0 && ret2 ==0));
    CloseTracker( tracker );
};

TEST_CASE( "Max Tack Events", "[Evnets]" ) {

    Tracker* tracker = (Tracker*)CreateTracker( 0, 0, 0, "MaxEventDataTest.json" );

    std::vector<EventData*> datas;

    for (int i = 0; i < 1024; i++) {
        EventData* data = CreateTelemetryEvent( 0 );
        int ret = TrackEvent( tracker, data );
    }
    EventData* data2 = CreateTelemetryEvent( 0 );
    int ret2 = TrackEvent( tracker, data2 );
    REQUIRE( ret2 == 1 );
    CloseTracker( tracker );
};


TEST_CASE( "Max Tack Events Flush push", "[Evnets]" ) {

    Tracker* tracker = (Tracker*)CreateTracker( 0, 0, 0, "MaxEventDataFlushTest.json" );

    std::vector<EventData*> datas;

    for (int i = 0; i < 1024; i++) {
        EventData* data = CreateTelemetryEvent( 0 );
        int ret = TrackEvent( tracker, data );
    }
    EventData* data2 = CreateTelemetryEvent( 0 );
    int ret2 = TrackEvent( tracker, data2 );

    Flush( tracker );

    data2 = CreateTelemetryEvent( 0 );
    ret2 = TrackEvent( tracker, data2 );
    REQUIRE( ret2 == 0 );
    CloseTracker( tracker );
};

