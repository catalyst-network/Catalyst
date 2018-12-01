// Generated by the protocol buffer compiler.  DO NOT EDIT!
// source: Mempool.proto

// This CPP symbol can be defined to use imports that match up to the framework
// imports needed when using CocoaPods.
#if !defined(GPB_USE_PROTOBUF_FRAMEWORK_IMPORTS)
 #define GPB_USE_PROTOBUF_FRAMEWORK_IMPORTS 0
#endif

#if GPB_USE_PROTOBUF_FRAMEWORK_IMPORTS
 #import <Protobuf/GPBProtocolBuffers.h>
#else
 #import "GPBProtocolBuffers.h"
#endif

#if GOOGLE_PROTOBUF_OBJC_VERSION < 30002
#error This file was generated by a newer version of protoc which is incompatible with your Protocol Buffer library sources.
#endif
#if 30002 < GOOGLE_PROTOBUF_OBJC_MIN_SUPPORTED_VERSION
#error This file was generated by an older version of protoc which is incompatible with your Protocol Buffer library sources.
#endif

// @@protoc_insertion_point(imports)

#pragma clang diagnostic push
#pragma clang diagnostic ignored "-Wdeprecated-declarations"

CF_EXTERN_C_BEGIN

@class Tx_Timestamp;

NS_ASSUME_NONNULL_BEGIN

#pragma mark - MempoolRoot

/**
 * Exposes the extension registry for this file.
 *
 * The base class provides:
 * @code
 *   + (GPBExtensionRegistry *)extensionRegistry;
 * @endcode
 * which is a @c GPBExtensionRegistry that includes all the extensions defined by
 * this file and all files that it depends on.
 **/
@interface MempoolRoot : GPBRootObject
@end

#pragma mark - Tx

typedef GPB_ENUM(Tx_FieldNumber) {
  Tx_FieldNumber_AddressSource = 1,
  Tx_FieldNumber_AddressDest = 2,
  Tx_FieldNumber_Signature = 3,
  Tx_FieldNumber_Amount = 4,
  Tx_FieldNumber_Fee = 5,
  Tx_FieldNumber_OutputAmount = 6,
  Tx_FieldNumber_InputAction = 7,
  Tx_FieldNumber_UnlockScript = 8,
  Tx_FieldNumber_UnlockingProgram = 9,
  Tx_FieldNumber_Updated = 10,
};

@interface Tx : GPBMessage

@property(nonatomic, readwrite, copy, null_resettable) NSString *addressSource;

@property(nonatomic, readwrite, copy, null_resettable) NSString *addressDest;

@property(nonatomic, readwrite, copy, null_resettable) NSString *signature;

@property(nonatomic, readwrite) uint32_t amount;

@property(nonatomic, readwrite) uint32_t fee;

@property(nonatomic, readwrite) uint32_t outputAmount;

@property(nonatomic, readwrite, copy, null_resettable) NSString *inputAction;

@property(nonatomic, readwrite, copy, null_resettable) NSString *unlockScript;

@property(nonatomic, readwrite, copy, null_resettable) NSString *unlockingProgram;

@property(nonatomic, readwrite, strong, null_resettable) Tx_Timestamp *updated;
/** Test to see if @c updated has been set. */
@property(nonatomic, readwrite) BOOL hasUpdated;

@end

#pragma mark - Tx_Timestamp

typedef GPB_ENUM(Tx_Timestamp_FieldNumber) {
  Tx_Timestamp_FieldNumber_Seconds = 1,
  Tx_Timestamp_FieldNumber_Nanos = 2,
};

@interface Tx_Timestamp : GPBMessage

@property(nonatomic, readwrite) int64_t seconds;

@property(nonatomic, readwrite) int32_t nanos;

@end

#pragma mark - Key

typedef GPB_ENUM(Key_FieldNumber) {
  Key_FieldNumber_HashedSignature = 1,
};

@interface Key : GPBMessage

@property(nonatomic, readwrite, copy, null_resettable) NSString *hashedSignature;

@end

NS_ASSUME_NONNULL_END

CF_EXTERN_C_END

#pragma clang diagnostic pop

// @@protoc_insertion_point(global_scope)
