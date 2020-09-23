# Changelog
All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## [0.5.0] - 2020-09-24

- Added new *ITransitionState* that acts as a non-blocker state between 2 different states

**Fixed**
- Fixed potential issue when *ITaskWait* would exit from a *INestState* or *ISplitState* without finishing the task	

## [0.4.0] - 2020-09-22

- Added the possibility to not execute *IStateExit.OnExit* on the current active state when leaving *IStateNest* or *IStateSplit*.
- Added the new *ITaskWaitState* to have a waiter for Task async methods. This state cannot have event triggers, for that use *IWaitState*

**Changed**
- Separated *IStateSplit.Split* into new method *IStateSplit.SplitFinal*

## [0.3.0] - 2020-09-06

- Added the possibility to trigger events without a target state. Only *InitialState*, *ChoiceState* & *LeaveState* don't allow it due to the nature of their behaviour.

## [0.2.0] - 2020-08-27

- Added the possibility to always execute the *FinalState* of a *NestState* and * *SplitState*

**Changed**
- Splitted the Unit tests in different files to help organization and readability. Now each state has it's own file with only it's set of tests

**Fixed**
- Now *NestState* and * *SplitState* properly execute their inner *OnExit* calls when they are setup in a chain of nested states

## [0.1.3] - 2020-01-06

- Removed package dependency

## [0.1.2] - 2020-01-06

- Removed Preview label out of the package version
- Added NSubstitute dependency for the Unit Tests

## [0.1.0] - 2020-01-05

- Initial submission for package distribution
